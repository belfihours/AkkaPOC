using Akka.Hosting;
using AkkaWordcounter2.App.Actors;
using AkkaWordcounter2.App.Config;
using AkkaWordcounter2.App.Messaging.Commands;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace AkkaWordCounter2.App.Tests;

public class ParserActorTest : Akka.Hosting.TestKit.TestKit
{
    private static readonly AbsoluteUri ParserActorUri = new(new Uri("https://getakka.net/"));
    public ParserActorTest(ITestOutputHelper output) : base(output: output)
    {
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient();
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.ConfigureLoggers(configBuilder =>
        {
            configBuilder.LogLevel = Akka.Event.LogLevel.DebugLevel;
        }).AddParserActors();
    }

    [Fact]
    public async Task WhenCalled_ShouldParseWords()
    {
        // arrange
        var sut = await ActorRegistry.GetAsync<ParserActor>();
        var expectResultsProbe = CreateTestProbe();

        // act
        sut.Tell(new DocumentCommands.ScanDocument(ParserActorUri), expectResultsProbe);
        
        // assert
        var msg = await expectResultsProbe.ExpectMsgAsync<DocumentEvents.WordsFound>();
        
        await expectResultsProbe.FishForMessageAsync(m => m is DocumentEvents.EndOfDocumentReached);
    }
}