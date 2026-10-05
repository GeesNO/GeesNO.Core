using System.Collections;
using System.Reflection;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Tests.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Events;

public sealed class RuntimeEventTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Event_rejects_non_positive_sequence(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationReceivedEvent(
                sequence,
                ObservabilityTestData.UtcNow,
                ObservabilityTestData.OperationId(),
                ObservabilityTestData.Capability()));
    }

    [Fact]
    public void Event_rejects_non_utc_timestamp()
    {
        var local = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<ArgumentException>(() =>
            new OperationReceivedEvent(
                1,
                local,
                ObservabilityTestData.OperationId(),
                ObservabilityTestData.Capability()));
    }

    [Fact]
    public void Operation_event_rejects_null_identity()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationReceivedEvent(
            1,
            ObservabilityTestData.UtcNow,
            null!,
            ObservabilityTestData.Capability()));
        Assert.Throws<ArgumentNullException>(() => new OperationReceivedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            null!));
    }

    [Fact]
    public void Received_event_contains_only_base_operation_data()
    {
        var operationId = ObservabilityTestData.OperationId();
        var capability = ObservabilityTestData.Capability();

        var runtimeEvent = new OperationReceivedEvent(
            7,
            ObservabilityTestData.UtcNow,
            operationId,
            capability);

        Assert.Equal(7, runtimeEvent.Sequence);
        Assert.Equal(ObservabilityTestData.UtcNow, runtimeEvent.OccurredAtUtc);
        Assert.Same(operationId, runtimeEvent.OperationId);
        Assert.Same(capability, runtimeEvent.Capability);
        Assert.DoesNotContain(runtimeEvent.GetType().GetProperties(), property =>
            property.Name is "ProviderId" or "DeclaredResultType" or "Error");
    }

    [Fact]
    public void Resolved_and_validated_events_require_provider()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationResolvedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            null!));
        Assert.Throws<ArgumentNullException>(() => new OperationValidatedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            null!));
    }

    [Fact]
    public void Started_and_succeeded_events_require_resolved_data()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationStartedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            null!,
            typeof(string)));
        Assert.Throws<ArgumentNullException>(() => new OperationStartedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            ObservabilityTestData.Provider(),
            null!));
        Assert.Throws<ArgumentNullException>(() => new OperationSucceededEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            null!,
            typeof(string)));
        Assert.Throws<ArgumentNullException>(() => new OperationSucceededEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            ObservabilityTestData.Provider(),
            null!));
    }

    [Fact]
    public void Resolved_validated_started_and_succeeded_preserve_exact_data()
    {
        var operationId = ObservabilityTestData.OperationId();
        var capability = ObservabilityTestData.Capability();
        var provider = ObservabilityTestData.Provider();
        var resolved = new OperationResolvedEvent(
            1, ObservabilityTestData.UtcNow, operationId, capability, provider);
        var validated = new OperationValidatedEvent(
            2, ObservabilityTestData.UtcNow, operationId, capability, provider);
        var started = new OperationStartedEvent(
            3, ObservabilityTestData.UtcNow, operationId, capability, provider, typeof(string));
        var succeeded = new OperationSucceededEvent(
            4, ObservabilityTestData.UtcNow, operationId, capability, provider, typeof(string));

        Assert.Same(provider, resolved.ProviderId);
        Assert.Same(provider, validated.ProviderId);
        Assert.Same(provider, started.ProviderId);
        Assert.Equal(typeof(string), started.DeclaredResultType);
        Assert.Same(provider, succeeded.ProviderId);
        Assert.Equal(typeof(string), succeeded.DeclaredResultType);
        Assert.All(new OperationRuntimeEvent[] { resolved, validated, started, succeeded }, runtimeEvent =>
        {
            Assert.Same(operationId, runtimeEvent.OperationId);
            Assert.Same(capability, runtimeEvent.Capability);
        });
    }

    [Theory]
    [InlineData((int)RuntimeExecutionStatus.Success)]
    [InlineData((int)RuntimeExecutionStatus.Cancelled)]
    public void Failed_event_rejects_non_failure_status(int statusValue)
    {
        var status = (RuntimeExecutionStatus)statusValue;
        Assert.Throws<ArgumentException>(() => CreateFailure(status));
    }

    [Fact]
    public void Failed_event_rejects_undefined_status()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFailure((RuntimeExecutionStatus)999));
    }

    [Theory]
    [InlineData((int)RuntimeExecutionStatus.FunctionalFailure)]
    [InlineData((int)RuntimeExecutionStatus.UnexpectedFailure)]
    public void Resolved_failures_require_provider_and_result_type(int statusValue)
    {
        var status = (RuntimeExecutionStatus)statusValue;
        var error = status == RuntimeExecutionStatus.UnexpectedFailure
            ? RuntimeInvocationErrors.UnexpectedToolFailure()
            : ObservabilityTestData.FunctionalError();

        Assert.Throws<ArgumentException>(() => CreateFailure(status, error));
        Assert.Throws<ArgumentException>(() => CreateFailure(
            status,
            error,
            ObservabilityTestData.Provider()));
    }

    [Fact]
    public void Contract_mismatch_accepts_only_approved_codes()
    {
        _ = CreateFailure(
            RuntimeExecutionStatus.ContractMismatch,
            RuntimeInvocationErrors.OperationContractMismatch());
        _ = CreateFailure(
            RuntimeExecutionStatus.ContractMismatch,
            RuntimeInvocationErrors.ToolContractMismatch());

        Assert.Throws<ArgumentException>(() => CreateFailure(
            RuntimeExecutionStatus.ContractMismatch,
            new Error("test.invalid", "Invalid mismatch.")));
    }

    [Fact]
    public void Unexpected_failure_accepts_only_sanitized_code()
    {
        _ = CreateFailure(
            RuntimeExecutionStatus.UnexpectedFailure,
            RuntimeInvocationErrors.UnexpectedToolFailure(),
            ObservabilityTestData.Provider(),
            typeof(string));

        Assert.Throws<ArgumentException>(() => CreateFailure(
            RuntimeExecutionStatus.UnexpectedFailure,
            ObservabilityTestData.FunctionalError(),
            ObservabilityTestData.Provider(),
            typeof(string)));
    }

    [Fact]
    public void Failed_event_requires_error()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationFailedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            RuntimeExecutionStatus.Rejected,
            null!));
    }

    [Fact]
    public void Cancelled_event_contains_no_error_or_exception()
    {
        var runtimeEvent = new OperationCancelledEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            ObservabilityTestData.Provider(),
            typeof(string));
        var properties = runtimeEvent.GetType().GetProperties();

        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(Error));
        Assert.DoesNotContain(properties, property => typeof(Exception).IsAssignableFrom(property.PropertyType));
    }

    [Fact]
    public void Event_models_are_immutable_and_contain_no_private_payload_types()
    {
        var eventTypes = typeof(RuntimeEvent).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(RuntimeEvent).Namespace
                && typeof(RuntimeEvent).IsAssignableFrom(type))
            .ToArray();
        var forbiddenTypes = new[]
        {
            typeof(IOperation),
            typeof(Exception),
            typeof(OperationCanceledException),
            typeof(object),
            typeof(IDictionary)
        };

        Assert.All(eventTypes.SelectMany(type => type.GetProperties(
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic)), property =>
        {
            Assert.Null(property.SetMethod);
            Assert.False(IsForbidden(property.PropertyType));
        });
        Assert.All(eventTypes.SelectMany(type => type.GetConstructors(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic)),
            constructor => Assert.DoesNotContain(constructor.GetParameters(), parameter =>
                IsForbidden(parameter.ParameterType)));

        bool IsForbidden(Type type)
        {
            return type == typeof(object)
                || typeof(IOperation).IsAssignableFrom(type)
                || typeof(Exception).IsAssignableFrom(type)
                || typeof(IDictionary).IsAssignableFrom(type)
                || forbiddenTypes.Contains(type);
        }
    }

    private static OperationFailedEvent CreateFailure(
        RuntimeExecutionStatus status,
        Error? error = null,
        GeesNO.Core.Contracts.Capabilities.ProviderId? providerId = null,
        Type? resultType = null)
    {
        return new OperationFailedEvent(
            1,
            ObservabilityTestData.UtcNow,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            status,
            error ?? ObservabilityTestData.FunctionalError(),
            providerId,
            resultType);
    }
}
