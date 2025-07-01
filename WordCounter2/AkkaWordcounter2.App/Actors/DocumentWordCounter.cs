using System.Collections.Immutable;
using Akka.Streams;
using AkkaWordcounter2.App.Messaging.Commands;
using AkkaWordcounter2.App.Messaging.Document;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Messaging.Helpers.Interface;
using Microsoft.Extensions.Logging;

namespace AkkaWordcounter2.App.Actors;

public sealed class DocumentWordCounter : UntypedActor
{
    private readonly AbsoluteUri _documentId;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    private readonly Dictionary<string, int> _wordsCount = [];
    private readonly HashSet<IActorRef> _subscribers = [];

    public DocumentWordCounter(AbsoluteUri documentId)
    {
        _documentId = documentId;
    }
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DocumentEvents.WordsFound wordsFound when wordsFound.DocumentId == _documentId:
            {
                _log.Debug("Found {0} words in document {1}", wordsFound.Tokens.Count, _documentId);
                foreach (var token in wordsFound.Tokens)
                {
                    if (!_wordsCount.TryAdd(token, 1))
                    {
                        _wordsCount[token]++;
                    }
                }
                break;
            }
            case DocumentQueries.FetchCounts subscribe when subscribe.DocumentId == _documentId:
            {
                _subscribers.Add(Sender);
                break;
            }
            case DocumentEvents.EndOfDocumentReached endOfDocumentReached
                when endOfDocumentReached.DocumentId == _documentId:
            {
                var output = new DocumentEvents.CountsTabulatedForDocument(_documentId,  _wordsCount.ToImmutableDictionary());
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(output);
                    subscriber.Tell("Hello tester!");
                }
                _subscribers.Clear();
                Become(Complete);
                break;
            }
            case IWithDocumentId withDocumentId when withDocumentId.DocumentId != _documentId:
            {
                _log.Warning("Received message for document {0} but I am responsible for document {1}",
                    withDocumentId.DocumentId, _documentId);
                break;
            }
            case ReceiveTimeout:
            {
                _log.Warning("Received timeout for document {0}", _documentId);
                Context.Stop(Self);
                break;
            }
            default:
            {
                Unhandled(message);
                break;
            }
        }
    }

    private void Complete(object message)
    {
        switch (message)
        {
            case DocumentQueries.FetchCounts:
            {
                Sender.Tell(new DocumentEvents.CountsTabulatedForDocument(_documentId,
                    _wordsCount.ToImmutableDictionary()));
                break;
            }
            case IWithDocumentId withDocumentId when  withDocumentId.DocumentId == _documentId:
            {
                _log.Warning("Received message for {0} but I already completed processing", _documentId);
                break;
            }
            case IWithDocumentId withDocumentId when withDocumentId.DocumentId != _documentId:
            {
                _log.Warning("Received message for document {0} but I am responsible for document {1}",
                    withDocumentId.DocumentId, _documentId);
                break;
            }
            case ReceiveTimeout:
            {
                Context.Stop(Self);
                break;
            }
            default:
            {
                Unhandled(message);
                break;
            }
        }
    }

    protected override void PreStart()
    {
        SetReceiveTimeout(TimeSpan.FromMinutes(1));
    }
}