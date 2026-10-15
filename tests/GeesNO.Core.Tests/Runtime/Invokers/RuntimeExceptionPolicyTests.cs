using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Invokers;

public sealed class RuntimeExceptionPolicyTests
{
    [Theory]
    [MemberData(nameof(FatalExceptions))]
    public void Fatal_exception_types_are_identified(Exception exception)
    {
        Assert.True(RuntimeExceptionPolicy.IsFatal(exception));
    }

    [Fact]
    public void Ordinary_exception_is_not_fatal()
    {
        Assert.False(RuntimeExceptionPolicy.IsFatal(new InvalidOperationException()));
    }

    [Fact]
    public void Null_exception_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => RuntimeExceptionPolicy.IsFatal(null!));
    }

    public static TheoryData<Exception> FatalExceptions => new(
        new Exception[]
        {
            new OutOfMemoryException(),
            new StackOverflowException(),
            new AccessViolationException()
        });
}
