using System.Reactive;
using System.Reactive.Linq;

namespace DemoApi.Extensions;

public static class ObservableExtensions
{
    public static IObservable<Unit> ToObservable(this CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            return Observable.Never<Unit>();
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Observable.Return(Unit.Default);
        }

        // use Create so that each .Subscribe is handled independently
        return Observable.Create<Unit>(observer =>
        {
            // Observable.Create does not handle errors on its own
            try
            {
                // return the registration because Dispose will unregister it
                return cancellationToken.Register(() =>
                {
                    // When the token is cancelled, publish and complete
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                });
            }
            catch (ObjectDisposedException e)
            {
                observer.OnError(e);

                throw;
            }
        });
    }

    public static IObservable<T> TakeUntilCancelled<T>(this IObservable<T> source, CancellationToken cancellationToken)
        => source.TakeUntil(cancellationToken.ToObservable());
}