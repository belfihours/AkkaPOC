using AkkaWordcounter2.App.Messaging.Helpers;
using AkkaWordcounter2.App.Messaging.Helpers.Interface;

namespace AkkaWordcounter2.App.Messaging.Document;

public static class DocumentQueries
{
    public sealed record FetchCounts(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed class SubscribeToAllCounts
    {
        public static readonly SubscribeToAllCounts Instance = new();
        private SubscribeToAllCounts(){}
    }
}