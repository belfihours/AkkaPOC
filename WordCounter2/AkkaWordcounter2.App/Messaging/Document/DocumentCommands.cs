using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Messaging.Helpers.Interface;

namespace AkkaWordcounter2.App.Messaging.Commands;

public static class DocumentCommands
{
    public sealed record ScanDocument(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed record ScanDocuments(IReadOnlyList<AbsoluteUri> DocumentIds);
}