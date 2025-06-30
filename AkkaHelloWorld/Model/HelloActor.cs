using Akka.Actor;
using Akka.Event;

namespace AkkaHelloWorld.Model;

public class HelloActor : UntypedActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case string stringMessage:
            {
                _log.Info("Received message {0}", stringMessage);
                if (!Sender.IsNobody()) // here you can see if the caller actually expects a response
                {
                    Sender.Tell(stringMessage);
                }
                break;
            }
            default:
                Unhandled(message);
                break;
        }
    }
}