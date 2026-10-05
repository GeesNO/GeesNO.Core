using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class RuntimeExecutionResultTests
{
    [Fact]
    public void Status_contains_exactly_the_approved_states()
    {
        Assert.Equal(
            ["Success", "Rejected", "FunctionalFailure", "ContractMismatch", "UnexpectedFailure", "Cancelled"],
            Enum.GetNames<RuntimeExecutionStatus>());
    }

    [Fact]
    public void Success_requires_resolved_identity_and_compatible_value()
    {
        var context = CreateContext();
        var value = new ExecutionTestResult("ok");

        var result = RuntimeExecutionResult.Success(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(ExecutionTestResult),
            value);

        Assert.Equal(RuntimeExecutionStatus.Success, result.Status);
        Assert.Same(value, result.SuccessfulValue);
        Assert.Null(result.Error);
        Assert.Null(result.InternalException);
        Assert.Throws<ArgumentException>(() => RuntimeExecutionResult.Success(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(string),
            value));
        Assert.Throws<ArgumentNullException>(() => RuntimeExecutionResult.Success(
            context.OperationId,
            context.Capability,
            null!,
            typeof(ExecutionTestResult),
            value));
    }

    [Theory]
    [MemberData(nameof(RejectedErrors))]
    public void Rejected_accepts_only_approved_codes_and_allows_early_null_metadata(Error error)
    {
        var context = CreateContext();

        var result = RuntimeExecutionResult.Rejected(
            context.OperationId,
            context.Capability,
            error);

        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Null(result.ProviderId);
        Assert.Null(result.DeclaredResultType);
        Assert.Same(error, result.Error);
        Assert.Throws<ArgumentException>(() => RuntimeExecutionResult.Rejected(
            context.OperationId,
            context.Capability,
            new Error("other.error", "Other")));
    }

    [Fact]
    public void Functional_failure_preserves_exact_error_and_requires_resolution()
    {
        var context = CreateContext();
        var error = new Error("tool.failed", "Expected failure");

        var result = RuntimeExecutionResult.FunctionalFailure(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(ExecutionTestResult),
            error);

        Assert.Same(error, result.Error);
        Assert.Null(result.SuccessfulValue);
        Assert.Null(result.InternalException);
        Assert.Throws<ArgumentNullException>(() => RuntimeExecutionResult.FunctionalFailure(
            context.OperationId,
            context.Capability,
            null!,
            typeof(ExecutionTestResult),
            error));
    }

    [Theory]
    [InlineData(RuntimeInvocationErrors.OperationContractMismatchCode)]
    [InlineData(RuntimeInvocationErrors.ToolContractMismatchCode)]
    public void Contract_mismatch_accepts_only_approved_codes(string code)
    {
        var context = CreateContext();

        var result = RuntimeExecutionResult.ContractMismatch(
            context.OperationId,
            context.Capability,
            new Error(code, "Mismatch"));

        Assert.Equal(RuntimeExecutionStatus.ContractMismatch, result.Status);
        Assert.Null(result.ProviderId);
        Assert.Throws<ArgumentException>(() => RuntimeExecutionResult.ContractMismatch(
            context.OperationId,
            context.Capability,
            new Error("other.error", "Other")));
    }

    [Fact]
    public void Unexpected_failure_is_sanitized_and_requires_resolution_and_exception()
    {
        var context = CreateContext();
        var exception = new InvalidOperationException("private detail");

        var result = RuntimeExecutionResult.UnexpectedFailure(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(ExecutionTestResult),
            exception);

        Assert.Equal(RuntimeInvocationErrors.UnexpectedToolFailureCode, result.Error?.Code);
        Assert.Equal("The tool failed unexpectedly.", result.Error?.Message);
        Assert.Same(exception, result.InternalException);
        Assert.Throws<ArgumentNullException>(() => RuntimeExecutionResult.UnexpectedFailure(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(ExecutionTestResult),
            null!));
    }

    [Fact]
    public void Cancelled_allows_early_or_resolved_metadata_without_error_or_value()
    {
        var context = CreateContext();
        var cancellation = new OperationCanceledException();

        var early = RuntimeExecutionResult.Cancelled(context.OperationId, context.Capability);
        var resolved = RuntimeExecutionResult.Cancelled(
            context.OperationId,
            context.Capability,
            context.ProviderId,
            typeof(ExecutionTestResult),
            cancellation);

        Assert.Null(early.ProviderId);
        Assert.Null(early.DeclaredResultType);
        Assert.Null(early.Error);
        Assert.Null(early.SuccessfulValue);
        Assert.Same(cancellation, resolved.InternalException);
    }

    [Fact]
    public void Mandatory_operation_and_capability_are_never_nullable()
    {
        var context = CreateContext();

        Assert.Throws<ArgumentNullException>(() => RuntimeExecutionResult.Cancelled(
            null!,
            context.Capability));
        Assert.Throws<ArgumentNullException>(() => RuntimeExecutionResult.Cancelled(
            context.OperationId,
            null!));
    }

    [Fact]
    public void Conversion_preserves_all_five_invocation_states_exactly()
    {
        var context = CreateContext();
        var value = new ExecutionTestResult("ok");
        var functionalError = new Error("tool.failed", "Expected");
        var exception = new InvalidOperationException("private");
        var cancellation = new OperationCanceledException();
        var invocations = new[]
        {
            InternalInvocationResult.Success(
                context.OperationId, context.ProviderId, context.Capability, typeof(ExecutionTestResult), value),
            InternalInvocationResult.FunctionalFailure(
                context.OperationId, context.ProviderId, context.Capability, typeof(ExecutionTestResult), functionalError),
            InternalInvocationResult.ContractMismatch(
                context.OperationId,
                context.ProviderId,
                context.Capability,
                typeof(ExecutionTestResult),
                RuntimeInvocationErrors.OperationContractMismatch()),
            InternalInvocationResult.UnexpectedFailure(
                context.OperationId,
                context.ProviderId,
                context.Capability,
                typeof(ExecutionTestResult),
                exception),
            InternalInvocationResult.Cancelled(
                context.OperationId,
                context.ProviderId,
                context.Capability,
                typeof(ExecutionTestResult),
                cancellation)
        };

        var converted = invocations.Select(RuntimeExecutionResult.FromInvocation).ToArray();

        Assert.Equal(
            [
                RuntimeExecutionStatus.Success,
                RuntimeExecutionStatus.FunctionalFailure,
                RuntimeExecutionStatus.ContractMismatch,
                RuntimeExecutionStatus.UnexpectedFailure,
                RuntimeExecutionStatus.Cancelled
            ],
            converted.Select(result => result.Status));

        for (var index = 0; index < invocations.Length; index++)
        {
            Assert.Equal(invocations[index].OperationId, converted[index].OperationId);
            Assert.Equal(invocations[index].ProviderId, converted[index].ProviderId);
            Assert.Equal(invocations[index].Capability, converted[index].Capability);
            Assert.Equal(invocations[index].DeclaredResultType, converted[index].DeclaredResultType);
            Assert.Same(invocations[index].SuccessfulValue, converted[index].SuccessfulValue);
            Assert.Same(invocations[index].Error, converted[index].Error);
            Assert.Same(invocations[index].InternalException, converted[index].InternalException);
        }
    }

    [Fact]
    public void Result_is_internal_sealed_immutable_and_not_exported()
    {
        var type = typeof(RuntimeExecutionResult);

        Assert.True(type.IsNotPublic);
        Assert.True(type.IsSealed);
        Assert.DoesNotContain(
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            property => property.SetMethod is not null);
        Assert.DoesNotContain(type.Assembly.GetExportedTypes(), exported => exported == type);
    }

    [Fact]
    public void Common_constructor_rejects_invalid_enum_and_invalid_terminal_states()
    {
        var context = CreateContext();

        AssertConstructorRejects(
            (RuntimeExecutionStatus)999,
            context,
            providerId: null,
            declaredResultType: null,
            successfulValue: null,
            error: null,
            internalException: null,
            typeof(ArgumentOutOfRangeException));
        AssertConstructorRejects(
            RuntimeExecutionStatus.Rejected,
            context,
            providerId: null,
            declaredResultType: null,
            successfulValue: null,
            error: null,
            internalException: null,
            typeof(ArgumentNullException));
        AssertConstructorRejects(
            RuntimeExecutionStatus.Cancelled,
            context,
            providerId: null,
            declaredResultType: null,
            successfulValue: "not allowed",
            error: null,
            internalException: null,
            typeof(ArgumentException));
        AssertConstructorRejects(
            RuntimeExecutionStatus.Cancelled,
            context,
            providerId: null,
            declaredResultType: null,
            successfulValue: null,
            RuntimeExecutionErrors.NotReady(),
            internalException: null,
            typeof(ArgumentException));
        AssertConstructorRejects(
            RuntimeExecutionStatus.UnexpectedFailure,
            context,
            context.ProviderId,
            typeof(ExecutionTestResult),
            successfulValue: null,
            RuntimeExecutionErrors.NotReady(),
            new InvalidOperationException(),
            typeof(ArgumentException));
    }

    public static TheoryData<Error> RejectedErrors => new()
    {
        RuntimeExecutionErrors.NotReady(),
        RuntimeExecutionErrors.ProviderNotFound(),
        RuntimeExecutionErrors.CapabilityNotFound(),
        RuntimeExecutionErrors.CapabilityNotExecutable(),
        RuntimeExecutionErrors.AmbiguousProvider(),
        RuntimeExecutionErrors.ToolNotFound(),
        RuntimeExecutionErrors.ConfirmationRequired()
    };

    private static ResultContext CreateContext()
    {
        return new ResultContext(
            OperationId.New(),
            ExecutionTestData.Reference(),
            ProviderId.Parse("provider.test"));
    }

    private static void AssertConstructorRejects(
        RuntimeExecutionStatus status,
        ResultContext context,
        ProviderId? providerId,
        Type? declaredResultType,
        object? successfulValue,
        Error? error,
        Exception? internalException,
        Type expectedExceptionType)
    {
        var constructor = Assert.Single(typeof(RuntimeExecutionResult).GetConstructors(
            BindingFlags.Instance | BindingFlags.NonPublic));
        var exception = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(
            [
                status,
                context.OperationId,
                context.Capability,
                providerId,
                declaredResultType,
                successfulValue,
                error,
                internalException
            ]));

        Assert.Equal(expectedExceptionType, exception.InnerException?.GetType());
    }

    private sealed record ResultContext(
        OperationId OperationId,
        CapabilityReference Capability,
        ProviderId ProviderId);
}
