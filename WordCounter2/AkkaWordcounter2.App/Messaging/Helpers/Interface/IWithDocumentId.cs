namespace AkkaWordcounter2.App.Messaging.Helpers.Interface;

public interface IWithDocumentId
{
    AbsoluteUri DocumentId { get; }
}