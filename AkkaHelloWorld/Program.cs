using Akka.Actor;
using Akka.Event;
using AkkaHelloWorld.Model;

ActorSystem myActorSystem = ActorSystem.Create("LocalSystem");
myActorSystem.Log.Info("Hello from the ActorSystem");


var props = Props.Create<HelloActor>();

var myActor = myActorSystem.ActorOf(props, "MyHelloActor");

myActor.Tell("Hello World!");

var response = await myActor.Ask<string>("What's up?");
Console.WriteLine(response);
await myActorSystem.Terminate();