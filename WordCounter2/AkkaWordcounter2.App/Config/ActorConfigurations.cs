using Akka.Hosting;
using Akka.Routing;
using AkkaWordcounter2.App.Actors;

namespace AkkaWordcounter2.App.Config;

public static class ActorConfigurations
{
    public static AkkaConfigurationBuilder AddApplicationActors(this AkkaConfigurationBuilder builder)
    {
        return builder
            .AddJobActor()
            .AddWordCounterActor()
            .AddParserActors();
    }
    
    public static AkkaConfigurationBuilder AddWordCounterActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, _) =>
        {
            var props = Props.Create<WordCounterManager>();
            var actor = system.ActorOf(props, "wordCounts");
            registry.Register<WordCounterManager>(actor);
        });
    }

    public static AkkaConfigurationBuilder AddParserActors(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, resolver) =>
        {
            var props = resolver.Props<ParserActor>()
                .WithRouter(new RoundRobinPool(5));
            var actor = system.ActorOf(props, "parserActor");
            registry.Register<ParserActor>(actor);
        });
    }

    public static AkkaConfigurationBuilder AddJobActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, resolver) =>
        {
            var props = resolver.Props<WordCountJobActor>();
            var actor = system.ActorOf(props, "jobActor");
            registry.Register<WordCountJobActor>(actor);
        });
    }
}