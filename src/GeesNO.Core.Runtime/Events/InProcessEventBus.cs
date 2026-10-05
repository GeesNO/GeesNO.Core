using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Runtime.Events;

internal sealed class InProcessEventBus
{
    private readonly IRuntimeEventObserver[] _observers;
    private readonly RuntimeObservabilityDiagnostics _diagnostics;

    internal InProcessEventBus(
        IEnumerable<IRuntimeEventObserver> observers,
        RuntimeObservabilityDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(observers);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _observers = observers.ToArray();
        if (_observers.Any(static observer => observer is null))
        {
            throw new ArgumentException("The observer collection cannot contain null elements.", nameof(observers));
        }

        _diagnostics = diagnostics;
    }

    internal async Task PublishAsync(RuntimeEvent runtimeEvent)
    {
        ArgumentNullException.ThrowIfNull(runtimeEvent);

        foreach (var observer in _observers)
        {
            try
            {
                var observation = observer.ObserveAsync(runtimeEvent);
                if (observation is null)
                {
                    throw new InvalidOperationException("The runtime event observer returned a null task.");
                }

                await observation.ConfigureAwait(false);
            }
            catch (Exception exception) when (!RuntimeExceptionPolicy.IsFatal(exception))
            {
                _diagnostics.Record(new RuntimeObserverFailure(
                    runtimeEvent.Sequence,
                    runtimeEvent.GetType(),
                    observer.GetType(),
                    exception));
            }
        }
    }
}
