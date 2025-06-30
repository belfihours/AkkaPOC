using Akka.Actor;

namespace WordCounter.Messages.Queries;

public static class CounterQueries
{
    public sealed record FetchCounts(IActorRef Subscriber);
}