using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using WordCounter.Messages.Commands;
using WordCounter.Messages.Queries;

namespace WordCounter.Actors;

public class CounterActor : UntypedActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, int> _tokenCounts = [];
    private bool _doneCounting = false;

    private readonly HashSet<IActorRef> _subscribers = [];
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case CounterCommands.CountTokens tokens:
            {
                foreach (var token in tokens.Tokens)
                {
                    // if not exists create, if exists add 1
                    if (!_tokenCounts.TryAdd(token, 1))
                    {
                        _tokenCounts[token] += 1;
                    }
                }
                break;
            }
            case CounterCommands.ExpectNoMoreTokens:
            {
                _doneCounting = true;
                _log.Info("Completed counting tokens - found {0} unique tokens",
                    _tokenCounts.Count);

                // ensure the output is immutable
                var totals = _tokenCounts.ToImmutableDictionary();
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(totals);
                }

                _subscribers.Clear();
                break;
            }
            case CounterQueries.FetchCounts fetchCounts when _doneCounting:
            {
                // you can instantly tell the results
                fetchCounts.Subscriber.Tell(_tokenCounts.ToImmutableDictionary());
                break;
            }
            case CounterQueries.FetchCounts fetchCounts:
            {
                _subscribers.Add(fetchCounts.Subscriber);
                break;
            }
            default:
            {
                Unhandled(message);
                break;
            }
        }
    }
}