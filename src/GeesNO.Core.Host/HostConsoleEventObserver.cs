using GeesNO.Core.Runtime.Events;

namespace GeesNO.Core.Host;

internal sealed class HostConsoleEventObserver : IRuntimeEventObserver
{
    private int _observedEvents;

    internal int ObservedEvents => Volatile.Read(ref _observedEvents);

    public Task ObserveAsync(RuntimeEvent runtimeEvent)
    {
        ArgumentNullException.ThrowIfNull(runtimeEvent);
        Interlocked.Increment(ref _observedEvents);
        return Task.CompletedTask;
    }
}
