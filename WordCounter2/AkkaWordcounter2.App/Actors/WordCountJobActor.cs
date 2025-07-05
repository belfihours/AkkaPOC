using System.Collections.Immutable;
using Akka.Hosting;
using AkkaWordcounter2.App.Messaging.Commands;
using AkkaWordcounter2.App.Messaging.Document;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Utiity;

namespace AkkaWordcounter2.App.Actors;

public class WordCountJobActor : UntypedActor, IWithStash, IWithTimers
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IRequiredActor<WordCounterManager> _wordCounterManager;
    private readonly IRequiredActor<ParserActor> _parserActor;

    public IStash Stash { get; set; } = null!;
    public ITimerScheduler Timers { get; set; } = null!;

    private readonly HashSet<IActorRef> _subscribers = [];
    private readonly Dictionary<AbsoluteUri, ProcessingStatus> _documentsToProcess = [];
    private readonly Dictionary<AbsoluteUri, ImmutableDictionary<string, int>> _wordsCounts = [];

    private enum ProcessingStatus
    {
        Processing = 0,
        Completed = 1,
        FailedError = 2,
        FailedTimeout = 3
    }

    public sealed class JobTimeOut
    {
        public static readonly JobTimeOut Instance = new();
        private JobTimeOut() { }
    }

    public WordCountJobActor(
        IRequiredActor<WordCounterManager> wordCounterManager,
        IRequiredActor<ParserActor> parserActor)
    {
        _wordCounterManager = wordCounterManager;
        _parserActor = parserActor;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DocumentCommands.ScanDocuments scan:
            {
                _log.Info("Received scan request for {0}", scan.DocumentIds.Count);
                foreach (var document in scan.DocumentIds)
                {
                    _documentsToProcess[document] = ProcessingStatus.Processing;
                    
                    // begin processing
                    _parserActor.ActorRef.Tell(new DocumentCommands.ScanDocument(document));
                    
                    // get back to us once processing is completed
                    _wordCounterManager.ActorRef.Tell(new DocumentQueries.FetchCounts(document));
                }
                Become(Running);
                Timers.StartSingleTimer("job-timeout",
                    JobTimeOut.Instance,
                    TimeSpan.FromSeconds(30));
                Stash.UnstashAll();
                break;
            }
            default:
            {
                // buffer any other message until the job starts
                Stash.Stash();
                break;
            }
        }
    }

    private void Running(object message)
    {
        switch (message)
        {
            case DocumentEvents.WordsFound found:
            {
                _wordCounterManager.ActorRef.Forward(found);
                break;
            }
            case DocumentEvents.EndOfDocumentReached end:
            {
                _wordCounterManager.ActorRef.Forward(end);
                break;
            }
            case DocumentEvents.CountsTabulatedForDocument countsDoc:
            {
                _log.Info("Received word counts for {0}", countsDoc.DocumentId);
                _wordsCounts[countsDoc.DocumentId] = countsDoc.WordFrequencies;
                _documentsToProcess[countsDoc.DocumentId] = ProcessingStatus.Completed;
                HandleJobCompletedMaybe();
                break;
            }
            case DocumentEvents.DocumentScanFailed failed:
            {
                _log.Error("Document scan failed for {0}: {1}", failed.DocumentId, failed.Reason);
                _documentsToProcess[failed.DocumentId] = ProcessingStatus.FailedError;
                HandleJobCompletedMaybe();
                break;
            }
            case JobTimeOut _:
            {
                _log.Error("Job timed out");

                foreach (var (document, status) in _documentsToProcess)
                {
                    if (status == ProcessingStatus.Processing)
                    {
                        _documentsToProcess[document] = ProcessingStatus.FailedTimeout;
                    }
                }
                HandleJobCompletedMaybe(true);
                break;
            }
            case DocumentQueries.SubscribeToAllCounts:
            {
                _subscribers.Add(Sender);
                break;
            }
            default:
            {
                Unhandled(message);
                break;
            }
        }
    }

    private void HandleJobCompletedMaybe(bool force = false)
    {
        if (!IsJobCompleted() && !force)
            return;
        
        // log statuses for each page
        foreach (var (document, status) in _documentsToProcess)
        {
            _log.Info("Document {0} status: {1}, total words: {2}", document, status,
                _wordsCounts[document].Values.Sum());
        }
        
        var mergedCounts = CollectionUtilities.MergeWordCounts(_wordsCounts.Values);
        var finalOutput = 
            new DocumentEvents.
                CountsTabulatedForDocuments(_documentsToProcess.Keys.ToList(), mergedCounts);

        foreach (var sub in _subscribers)
        {
            sub.Tell(finalOutput);
        }
    }

    private bool IsJobCompleted() => _documentsToProcess.Values
        .All(d => d > ProcessingStatus.Processing);
}