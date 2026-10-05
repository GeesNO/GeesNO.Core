namespace GeesNO.Core.Runtime.Events;

internal interface IRuntimeEventObserver
{
    Task ObserveAsync(RuntimeEvent runtimeEvent);
}
