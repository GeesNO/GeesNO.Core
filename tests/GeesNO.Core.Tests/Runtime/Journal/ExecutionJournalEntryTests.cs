using System.Collections;
using System.Reflection;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Invokers;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Tests.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Journal;

public sealed class ExecutionJournalEntryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Entry_rejects_non_positive_sequence(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            sequence,
            RuntimeExecutionStatus.Cancelled));
    }

    [Fact]
    public void Entry_rejects_null_identity()
    {
        Assert.Throws<ArgumentNullException>(() => new ExecutionJournalEntry(
            1,
            null!,
            ObservabilityTestData.Capability(),
            RuntimeExecutionStatus.Cancelled,
            ObservabilityTestData.UtcNow,
            TimeSpan.Zero));
        Assert.Throws<ArgumentNullException>(() => new ExecutionJournalEntry(
            1,
            ObservabilityTestData.OperationId(),
            null!,
            RuntimeExecutionStatus.Cancelled,
            ObservabilityTestData.UtcNow,
            TimeSpan.Zero));
    }

    [Fact]
    public void Entry_requires_utc_start_and_non_negative_duration()
    {
        var local = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<ArgumentException>(() => new ExecutionJournalEntry(
            1,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            RuntimeExecutionStatus.Cancelled,
            local,
            TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExecutionJournalEntry(
            1,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            RuntimeExecutionStatus.Cancelled,
            ObservabilityTestData.UtcNow,
            TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void Entry_rejects_undefined_status()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            1,
            (RuntimeExecutionStatus)999));
    }

    [Theory]
    [InlineData((int)RuntimeExecutionStatus.Success)]
    [InlineData((int)RuntimeExecutionStatus.Rejected)]
    [InlineData((int)RuntimeExecutionStatus.FunctionalFailure)]
    [InlineData((int)RuntimeExecutionStatus.ContractMismatch)]
    [InlineData((int)RuntimeExecutionStatus.UnexpectedFailure)]
    [InlineData((int)RuntimeExecutionStatus.Cancelled)]
    public void Entry_accepts_every_terminal_state(int statusValue)
    {
        var status = (RuntimeExecutionStatus)statusValue;
        var entry = ObservabilityTestData.JournalEntry(status: status);

        Assert.Equal(status, entry.Status);
        Assert.True(entry.Sequence > 0);
        Assert.Equal(TimeSpan.Zero, entry.StartedAtUtc.Offset);
        Assert.True(entry.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public void Success_requires_resolved_data_and_no_error()
    {
        Assert.Throws<ArgumentException>(() => Create(1, RuntimeExecutionStatus.Success));
        Assert.Throws<ArgumentException>(() => Create(
            1,
            RuntimeExecutionStatus.Success,
            ObservabilityTestData.Provider()));
        Assert.Throws<ArgumentException>(() => Create(
            1,
            RuntimeExecutionStatus.Success,
            ObservabilityTestData.Provider(),
            typeof(string),
            ObservabilityTestData.FunctionalError()));
    }

    [Fact]
    public void Rejected_requires_error_but_allows_unresolved_data()
    {
        Assert.Throws<ArgumentException>(() => Create(1, RuntimeExecutionStatus.Rejected));

        var entry = Create(
            1,
            RuntimeExecutionStatus.Rejected,
            error: ObservabilityTestData.FunctionalError());

        Assert.Null(entry.ProviderId);
        Assert.Null(entry.DeclaredResultType);
    }

    [Fact]
    public void Functional_failure_requires_resolved_data_and_error()
    {
        Assert.Throws<ArgumentException>(() => Create(
            1,
            RuntimeExecutionStatus.FunctionalFailure,
            error: ObservabilityTestData.FunctionalError()));
        Assert.Throws<ArgumentException>(() => Create(
            1,
            RuntimeExecutionStatus.FunctionalFailure,
            ObservabilityTestData.Provider(),
            typeof(string)));
    }

    [Fact]
    public void Contract_mismatch_requires_an_approved_code()
    {
        _ = Create(
            1,
            RuntimeExecutionStatus.ContractMismatch,
            error: RuntimeInvocationErrors.OperationContractMismatch());
        _ = Create(
            2,
            RuntimeExecutionStatus.ContractMismatch,
            error: RuntimeInvocationErrors.ToolContractMismatch());

        Assert.Throws<ArgumentException>(() => Create(
            3,
            RuntimeExecutionStatus.ContractMismatch,
            error: ObservabilityTestData.FunctionalError()));
        Assert.Throws<ArgumentException>(() => Create(
            4,
            RuntimeExecutionStatus.ContractMismatch));
    }

    [Fact]
    public void Unexpected_failure_requires_resolved_data_and_sanitized_code()
    {
        Assert.Throws<ArgumentException>(() => Create(
            1,
            RuntimeExecutionStatus.UnexpectedFailure,
            error: RuntimeInvocationErrors.UnexpectedToolFailure()));
        Assert.Throws<ArgumentException>(() => Create(
            2,
            RuntimeExecutionStatus.UnexpectedFailure,
            ObservabilityTestData.Provider(),
            typeof(string),
            ObservabilityTestData.FunctionalError()));
    }

    [Fact]
    public void Cancelled_requires_no_error_and_allows_resolved_data()
    {
        _ = Create(1, RuntimeExecutionStatus.Cancelled);
        var resolved = Create(
            2,
            RuntimeExecutionStatus.Cancelled,
            ObservabilityTestData.Provider(),
            typeof(string));

        Assert.NotNull(resolved.ProviderId);
        Assert.Throws<ArgumentException>(() => Create(
            3,
            RuntimeExecutionStatus.Cancelled,
            error: ObservabilityTestData.FunctionalError()));
    }

    [Fact]
    public void Entry_is_immutable_and_has_no_private_payload_properties()
    {
        var properties = typeof(ExecutionJournalEntry).GetProperties(
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic);

        Assert.All(properties, property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(properties, property =>
            property.PropertyType == typeof(object)
            || typeof(IOperation).IsAssignableFrom(property.PropertyType)
            || typeof(Exception).IsAssignableFrom(property.PropertyType)
            || typeof(IDictionary).IsAssignableFrom(property.PropertyType));
    }

    private static ExecutionJournalEntry Create(
        long sequence,
        RuntimeExecutionStatus status,
        GeesNO.Core.Contracts.Capabilities.ProviderId? providerId = null,
        Type? resultType = null,
        Error? error = null)
    {
        return new ExecutionJournalEntry(
            sequence,
            ObservabilityTestData.OperationId(),
            ObservabilityTestData.Capability(),
            status,
            ObservabilityTestData.UtcNow,
            TimeSpan.Zero,
            providerId,
            resultType,
            error);
    }
}
