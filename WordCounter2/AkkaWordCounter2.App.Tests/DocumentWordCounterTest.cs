using Akka.Actor;
using Akka.TestKit.Xunit2;
using AkkaWordcounter2.App.Actors;
using AkkaWordcounter2.App.Messaging.Document;
using AkkaWordcounter2.App.Messaging.Events;
using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Messaging.Helpers.Interface;
using FluentAssertions;
using Xunit.Abstractions;

namespace AkkaWordCounter2.App.Tests;

public class DocumentWordCounterTest : TestKit
{
    private static readonly Akka.Configuration.Config Config = "akka.loglevel=DEBUG"; // here you low the log-level for debug purposes
    private static readonly AbsoluteUri TestDocumentUri = new(new Uri("http://example.com/test"));
    
    public DocumentWordCounterTest(ITestOutputHelper output) : base(Config, output: output)
    {
        
    }

    [Fact]
    public async Task ShouldProcessWordCountsCorrectly()
    {
        // Arrange
        var props = Props.Create(() => new DocumentWordCounter(TestDocumentUri));
        var actor = Sys.ActorOf(props);

        IReadOnlyList<IWithDocumentId> messages =
        [
            new DocumentEvents.WordsFound(TestDocumentUri, ["hello", "world"]),
            new DocumentEvents.WordsFound(TestDocumentUri, ["bar", "foo"]),
            new DocumentEvents.WordsFound(TestDocumentUri, ["HeLlo", "wOrld"]),
            new DocumentEvents.EndOfDocumentReached(TestDocumentUri)
        ];

        // TestActor is the default sender in this case, here is specified just to make it clearer
        actor.Tell(new DocumentQueries.FetchCounts(TestDocumentUri), TestActor);
        
        // Act
        foreach (var message in messages)
        {
            actor.Tell(message);
        }
        
        // Assert
        await WithinAsync(TimeSpan.FromSeconds(5), async () =>
        {
            //both calls has to be completed in x seconds, just a wrapper
            var response = await ExpectMsgAsync<DocumentEvents.CountsTabulatedForDocument>();
            response.WordFrequencies.Count.Should().Be(6); // case-sensitive
            await ExpectMsgAsync<string>();
        });
        // var response = await ExpectMsgAsync<DocumentEvents.CountsTabulatedForDocument>(TimeSpan.FromSeconds(1)); // default is 3 seconds
        // response.WordFrequencies.Count.Should().Be(6); // case-sensitive
    }
}