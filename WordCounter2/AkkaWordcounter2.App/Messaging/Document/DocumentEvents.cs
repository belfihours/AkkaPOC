using System.Collections.Immutable;
using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Messaging.Helpers.Interface;

namespace AkkaWordcounter2.App.Messaging.Events;

public static class DocumentEvents
{
    public sealed record DocumentScanFailed(AbsoluteUri DocumentId, string Reason) : IWithDocumentId;

    public sealed record WordsFound(AbsoluteUri DocumentId, IReadOnlyList<string> Tokens) : IWithDocumentId;

    public sealed record EndOfDocumentReached(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed record CountsTabulatedForDocument(AbsoluteUri DocumentId, ImmutableDictionary<string, int> WordFrequencies)
        : IWithDocumentId;

    public sealed record CountsTabulatedForDocuments(
        IReadOnlyList<AbsoluteUri> Documents,
        IImmutableDictionary<string, int> WordFrequencies);
}