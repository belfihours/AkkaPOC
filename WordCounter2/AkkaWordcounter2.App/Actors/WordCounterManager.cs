using AkkaWordcounter2.App.Messaging.Helpers.Interface;
using System.Drawing.Text;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace AkkaWordcounter2.App.Actors;

public sealed class WordCounterManager : ReceiveActor
{
    public WordCounterManager()
    {
        Receive<IWithDocumentId>(s =>
        {
            string childName = $"word-counter-{HttpUtility.UrlEncode(s.DocumentId.ToString())}";
            var child = Context.Child(childName);
            if (child.IsNobody())
            {
                // starts the child if it doesn't exists yet
                child = Context.ActorOf(Props.Create(()=>new DocumentWordCounter(s.DocumentId)), childName);
            }
            child.Forward(s);
        });
    }
}
