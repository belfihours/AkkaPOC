using Akka.Streams.Actors;
using static Akka.Streams.FlowMonitor;

namespace AkkaWordcounter2.App.FooClasses;

internal class EncapsulateActor : ReceiveActor, IWithStash
{

    private Task _runningTask;
    private CancellationTokenSource _cancel;

    public IStash Stash { get; set; }

    public EncapsulateActor()
    {
        _cancel = new CancellationTokenSource();
        Ready();
    }

    private void Ready()
    {
        Receive<StartMessage>(s =>
        {
            var self = Self; // closure
            _runningTask = Task.Run(() =>
            {
                // ... work
            }, _cancel.Token).ContinueWith(x =>
            {
                // TODO: understand how to do this part
                if (x.IsCanceled || x.IsFaulted)
                {
                    return new Task<Failed>(() => new Failed(new Exception()));
                }
                return x;
            }, TaskContinuationOptions.ExecuteSynchronously)
            .PipeTo(self);

            // switch behavior
            Become(Working);
        });

    }

    private void Working()
    {
        Receive<Cancel>(cancel => {
            _cancel.Cancel(); // cancel work
            BecomeReady();
        });
        Receive<Failed>(f => BecomeReady());
        Receive<Finished>(f => BecomeReady());
        ReceiveAny(o => Stash.Stash());
    }

    private void BecomeReady()
    {
        _cancel = new CancellationTokenSource();
        Stash.UnstashAll();
        Become(Ready);
    }
}

internal record StartMessage() { }
