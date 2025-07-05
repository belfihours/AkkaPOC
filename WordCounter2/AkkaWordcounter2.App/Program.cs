using Akka.Hosting;
using AkkaWordcounter2.App;
using AkkaWordcounter2.App.Actors;
using AkkaWordcounter2.App.Config;
using AkkaWordcounter2.App.Messaging.Commands;
using AkkaWordcounter2.App.Messaging.Document;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var hostBuilder = new HostBuilder();

hostBuilder
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json"
                , optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    { 
        services.AddWordCounterSettings();
        services.AddHttpClient();
        services.AddAkka("MyActorSystem", (builder, sp) =>
        {
            builder
                .ConfigureLoggers(logConfig =>
                {
                    logConfig.AddLoggerFactory();
                    logConfig.LogLevel = LogLevel.InfoLevel;
                })
                .AddApplicationActors()
                .AddStartup(async (system, registry) =>
                {
                    var settings = sp.GetRequiredService<IOptions<WordCounterSettings>>();
                    var jobActor = await registry.GetAsync<WordCountJobActor>();
                    var uris = settings.Value.DocumentUris
                        .Select(u => new AbsoluteUri(new Uri(u)));
                    jobActor.Tell(new DocumentCommands.ScanDocuments(uris.ToArray()));
                    
                    // wait for the job to complete
                    var counts = await
                        jobActor.Ask<DocumentEvents.CountsTabulatedForDocuments>(
                            DocumentQueries.SubscribeToAllCounts.Instance, 
                            TimeSpan.FromMinutes(1));
                    foreach (var (word, count) in counts.WordFrequencies)
                    {
                        Console.WriteLine($"Word count for {word}: {count}");
                    }
                });
        });
});

var host = hostBuilder.Build();

await host.RunAsync();