namespace GeesNO.Core.Runtime.Invokers;

internal static class RuntimeExceptionPolicy
{
    internal static bool IsFatal(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException;
    }
}
