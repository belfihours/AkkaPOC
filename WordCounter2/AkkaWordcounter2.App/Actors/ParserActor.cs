using System.Net.WebSockets;
using AkkaWordcounter2.App.Messaging.Commands;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Utiity;
using HtmlAgilityPack;

namespace AkkaWordcounter2.App.Actors;

public sealed class ParserActor : UntypedActor
{
    private const int ChunkSize = 20;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public ParserActor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        
    }
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DocumentCommands.ScanDocument document:
            {
                RunTask(async () =>
                {
                    try
                    {
                        var textFeatures = await HandleDocument(document.DocumentId);
                        foreach (var f in textFeatures)
                        {
                            Sender.Tell(new DocumentEvents.WordsFound(document.DocumentId, f));
                        }

                        Sender.Tell(new DocumentEvents.EndOfDocumentReached(document.DocumentId));
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error processing document {0}", document.DocumentId);
                        Sender.Tell(new DocumentEvents.DocumentScanFailed(document.DocumentId, ex.Message));
                    }
                });
                break;
            }
        }
    }

    private async Task<IEnumerable<string[]>> HandleDocument(AbsoluteUri uri)
    {
        using var requestToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedToken = CancellationTokenSource
            .CreateLinkedTokenSource(requestToken.Token, _cancellationTokenSource.Token);
        
        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(uri.Value, linkedToken.Token);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException();
        var content = await response.Content.ReadAsStringAsync(linkedToken.Token);
        var document = new HtmlDocument();
        document.LoadHtml(content);
        
        // extract text
        var text = TextExtractor.ExtractText(document);
        return text.SelectMany(TextExtractor.ExtractTokens).Chunk(ChunkSize);
    }

    protected override void PostStop()
    {
        _cancellationTokenSource.Cancel();
    }
}