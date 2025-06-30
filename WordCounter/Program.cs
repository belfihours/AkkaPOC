// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Event;
using WordCounter.Actors;
using WordCounter.Messages.Commands;
using WordCounter.Messages.Queries;

ActorSystem myActorSystem = ActorSystem.Create("LocalSystem");
myActorSystem.Log.Info("Hello from the ActorSystem");

var counterActor = myActorSystem.ActorOf(Props.Create<CounterActor>(), "CounterActor");
var parserActor = myActorSystem.ActorOf(Props.Create<ParserActor>(counterActor), "ParserActor");

Task<IDictionary<string, int>> completionPromise = counterActor
    .Ask<IDictionary<string, int>>(refActor=>new CounterQueries.FetchCounts(refActor), null, CancellationToken.None);    

parserActor.Tell(new DocumentCommands.ProcessDocument(
    """
    This go is a test of the Akka.NET Word Counter. 
    I would go go 
    8 8 8 8 8 8 8 8 8
    """
    ));
    
var counts = await completionPromise;

foreach (var item in counts)
{
    myActorSystem.Log.Info($"{item.Key}: {item.Value} instances");
}

await myActorSystem.Terminate();