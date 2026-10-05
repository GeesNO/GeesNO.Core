namespace GeesNO.Core.Runtime.Observability;

internal abstract class RuntimeObservabilityFailure
{
    private protected RuntimeObservabilityFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Exception = exception;
    }

    internal Exception Exception { get; }
}
