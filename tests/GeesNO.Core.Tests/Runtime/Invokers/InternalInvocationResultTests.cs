using System.Reflection;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Invokers;

public sealed class InternalInvocationResultTests
{
    [Fact]
    public void Status_contains_exactly_the_approved_states()
    {
        Assert.Equal(
            ["Success", "FunctionalFailure", "ContractMismatch", "UnexpectedFailure", "Cancelled"],
            Enum.GetNames<InternalInvocationStatus>());
    }

    [Fact]
    public void Success_accepts_compatible_reference_and_boxed_value_types()
    {
        var context = CreateContext();

        var referenceResult = InternalInvocationResult.Success(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            "value");
        var valueResult = InternalInvocationResult.Success(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(int),
            42);

        Assert.Equal("value", referenceResult.SuccessfulValue);
        Assert.Equal(42, valueResult.SuccessfulValue);
        Assert.Null(referenceResult.Error);
        Assert.Null(referenceResult.InternalException);
    }

    [Fact]
    public void Success_rejects_null_or_incompatible_value()
    {
        var context = CreateContext();

        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.Success(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            null!));
        Assert.Throws<ArgumentException>(() => InternalInvocationResult.Success(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            42));
    }

    [Fact]
    public void Functional_failure_requires_and_preserves_error()
    {
        var context = CreateContext();
        var error = new Error("expected.failure", "Expected failure");

        var result = InternalInvocationResult.FunctionalFailure(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            error);

        Assert.Equal(InternalInvocationStatus.FunctionalFailure, result.Status);
        Assert.Same(error, result.Error);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.FunctionalFailure(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            null!));
    }

    [Theory]
    [InlineData(RuntimeInvocationErrors.OperationContractMismatchCode)]
    [InlineData(RuntimeInvocationErrors.ToolContractMismatchCode)]
    public void Contract_mismatch_accepts_only_approved_error_codes(string code)
    {
        var context = CreateContext();

        var result = InternalInvocationResult.ContractMismatch(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            new Error(code, "Mismatch"));

        Assert.Equal(InternalInvocationStatus.ContractMismatch, result.Status);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
        Assert.Throws<ArgumentException>(() => InternalInvocationResult.ContractMismatch(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            new Error("other.code", "Other")));
    }

    [Fact]
    public void Unexpected_failure_uses_sanitized_error_and_requires_exception()
    {
        var context = CreateContext();
        var exception = new InvalidOperationException("private detail");

        var result = InternalInvocationResult.UnexpectedFailure(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            exception);

        Assert.Equal(InternalInvocationStatus.UnexpectedFailure, result.Status);
        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, result.Error?.Code);
        Assert.Equal("The tool failed unexpectedly.", result.Error?.Message);
        Assert.Same(exception, result.InternalException);
        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.UnexpectedFailure(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            null!));
    }

    [Fact]
    public void Cancelled_contains_no_value_or_error_and_optionally_retains_cancellation()
    {
        var context = CreateContext();
        var exception = new OperationCanceledException();

        var withoutException = InternalInvocationResult.Cancelled(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string));
        var withException = InternalInvocationResult.Cancelled(
            context.OperationId,
            context.Key.ProviderId,
            context.Key.Capability,
            typeof(string),
            exception);

        Assert.Equal(InternalInvocationStatus.Cancelled, withoutException.Status);
        Assert.Null(withoutException.SuccessfulValue);
        Assert.Null(withoutException.Error);
        Assert.Null(withoutException.InternalException);
        Assert.Same(exception, withException.InternalException);
    }

    [Fact]
    public void Mandatory_identity_and_type_properties_reject_null()
    {
        var context = CreateContext();

        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.Success(
            null!, context.Key.ProviderId, context.Key.Capability, typeof(string), "value"));
        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.Success(
            context.OperationId, null!, context.Key.Capability, typeof(string), "value"));
        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.Success(
            context.OperationId, context.Key.ProviderId, null!, typeof(string), "value"));
        Assert.Throws<ArgumentNullException>(() => InternalInvocationResult.Success(
            context.OperationId, context.Key.ProviderId, context.Key.Capability, null!, "value"));
    }

    [Fact]
    public void Common_constructor_rejects_an_undefined_status()
    {
        var context = CreateContext();
        var constructor = Assert.Single(typeof(InternalInvocationResult).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic));

        var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(
            [
                (InternalInvocationStatus)999,
                context.OperationId,
                context.Key.ProviderId,
                context.Key.Capability,
                typeof(string),
                "value",
                null,
                null
            ]));

        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Fact]
    public void Common_constructor_rejects_invalid_mismatch_unexpected_and_cancelled_states()
    {
        var context = CreateContext();
        var exception = new InvalidOperationException();

        AssertConstructorRejects(
            InternalInvocationStatus.ContractMismatch,
            context,
            successfulValue: null,
            error: null,
            internalException: null,
            typeof(ArgumentNullException));
        AssertConstructorRejects(
            InternalInvocationStatus.UnexpectedFailure,
            context,
            successfulValue: null,
            RuntimeInvocationErrors.UnexpectedToolFailure(),
            internalException: null,
            typeof(ArgumentNullException));
        AssertConstructorRejects(
            InternalInvocationStatus.UnexpectedFailure,
            context,
            successfulValue: null,
            error: null,
            exception,
            typeof(ArgumentNullException));
        AssertConstructorRejects(
            InternalInvocationStatus.Cancelled,
            context,
            successfulValue: "value",
            error: null,
            internalException: null,
            typeof(ArgumentException));
        AssertConstructorRejects(
            InternalInvocationStatus.Cancelled,
            context,
            successfulValue: null,
            new Error("cancelled.error", "Not allowed"),
            internalException: null,
            typeof(ArgumentException));
        AssertConstructorRejects(
            InternalInvocationStatus.Cancelled,
            context,
            successfulValue: null,
            error: null,
            exception,
            typeof(ArgumentException));
    }

    [Fact]
    public void Result_is_internal_sealed_immutable_and_not_exported()
    {
        var type = typeof(InternalInvocationResult);

        Assert.True(type.IsNotPublic);
        Assert.True(type.IsSealed);
        Assert.DoesNotContain(type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.SetMethod is not null);
        Assert.DoesNotContain(type.Assembly.GetExportedTypes(), exported => exported == type);
    }

    private static InvocationResultContext CreateContext()
    {
        return new InvocationResultContext(OperationId.New(), InvokerTestData.Key());
    }

    private static void AssertConstructorRejects(
        InternalInvocationStatus status,
        InvocationResultContext context,
        object? successfulValue,
        Error? error,
        Exception? internalException,
        Type expectedExceptionType)
    {
        var constructor = Assert.Single(typeof(InternalInvocationResult).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic));
        var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(
            [
                status,
                context.OperationId,
                context.Key.ProviderId,
                context.Key.Capability,
                typeof(string),
                successfulValue,
                error,
                internalException
            ]));

        Assert.Equal(expectedExceptionType, exception.InnerException?.GetType());
    }

    private sealed record InvocationResultContext(
        GeesNO.Core.Contracts.Operations.OperationId OperationId,
        GeesNO.Core.Runtime.Catalog.ProviderCapabilityKey Key);
}
