using Akka.Actor;
using Akka.Event;
using WordCounter.Messages.Commands;

namespace WordCounter.Actors;

public class ParserActor: UntypedActor
{
    private readonly ILoggingAdapter _log =  Context.GetLogger();
    private readonly IActorRef _countingActor;

    public ParserActor(IActorRef countingActor)
    {
        _countingActor = countingActor ??  throw new ArgumentNullException(nameof(countingActor));
    }
    
    private const int TokenBatchSize = 10;
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case DocumentCommands.ProcessDocument processDocument:
            {
                // chunk tokens into buckets of 10
                foreach (var tokenBatch in processDocument.RawText.Split(" ").Chunk(TokenBatchSize))
                {
                    _countingActor.Tell(new CounterCommands.CountTokens(tokenBatch));
                }
                
                //we finished
                _countingActor.Tell(new CounterCommands.ExpectNoMoreTokens());
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