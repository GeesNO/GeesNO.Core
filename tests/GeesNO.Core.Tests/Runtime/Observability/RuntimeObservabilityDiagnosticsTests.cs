using System.Reflection;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Observability;

public sealed class RuntimeObservabilityDiagnosticsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Diagnostics_rejects_non_positive_capacity(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RuntimeObservabilityDiagnostics(capacity));
    }

    [Fact]
    public void Diagnostics_uses_bounded_fifo_retention()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics(2);
        var first = ObserverFailure(1);
        var second = ObserverFailure(2);
        var third = ObserverFailure(3);

        diagnostics.Record(first);
        diagnostics.Record(second);
        diagnostics.Record(third);

        Assert.Equal([second, third], diagnostics.GetFailures());
    }

    [Fact]
    public void Snapshot_is_defensive_and_read_only()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics(2);
        diagnostics.Record(ObserverFailure(1));
        var snapshot = diagnostics.GetFailures();

        diagnostics.Record(ObserverFailure(2));

        Assert.Single(snapshot);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<RuntimeObservabilityFailure>)snapshot).Add(ObserverFailure(3)));
    }

    [Fact]
    public void Diagnostics_rejects_null_failure()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();

        Assert.Throws<ArgumentNullException>(() => diagnostics.Record(null!));
    }

    [Fact]
    public void Diagnostics_supports_concurrent_writers_without_exceeding_capacity()
    {
        const int capacity = 64;
        var diagnostics = new RuntimeObservabilityDiagnostics(capacity);

        Parallel.For(1, 257, sequence => diagnostics.Record(ObserverFailure(sequence)));

        var snapshot = diagnostics.GetFailures();
        Assert.Equal(capacity, snapshot.Count);
        Assert.Equal(capacity, snapshot.Distinct().Count());
    }

    [Fact]
    public void Observer_failure_preserves_exact_private_data()
    {
        var exception = new InvalidOperationException("private");

        var failure = new RuntimeObserverFailure(
            9,
            typeof(OperationReceivedEvent),
            typeof(TestObserver),
            exception);

        Assert.Equal(9, failure.EventSequence);
        Assert.Equal(typeof(OperationReceivedEvent), failure.EventType);
        Assert.Equal(typeof(TestObserver), failure.ObserverType);
        Assert.Same(exception, failure.Exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Observer_failure_rejects_non_positive_sequence(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuntimeObserverFailure(
            sequence,
            typeof(OperationReceivedEvent),
            typeof(TestObserver),
            new InvalidOperationException()));
    }

    [Fact]
    public void Observer_failure_rejects_null_types_and_exception()
    {
        Assert.Throws<ArgumentNullException>(() => new RuntimeObserverFailure(
            1,
            null!,
            typeof(TestObserver),
            new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() => new RuntimeObserverFailure(
            1,
            typeof(OperationReceivedEvent),
            null!,
            new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() => new RuntimeObserverFailure(
            1,
            typeof(OperationReceivedEvent),
            typeof(TestObserver),
            null!));
    }

    [Fact]
    public void Infrastructure_failure_validates_component_and_optional_sequence()
    {
        var exception = new InvalidOperationException("private");
        var withoutSequence = new RuntimeInfrastructureFailure(
            RuntimeInfrastructureComponent.Clock,
            null,
            exception);
        var withSequence = new RuntimeInfrastructureFailure(
            RuntimeInfrastructureComponent.Journal,
            7,
            exception);

        Assert.Null(withoutSequence.RelatedSequence);
        Assert.Equal(RuntimeInfrastructureComponent.Journal, withSequence.Component);
        Assert.Equal(7, withSequence.RelatedSequence);
        Assert.Same(exception, withSequence.Exception);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuntimeInfrastructureFailure(
            (RuntimeInfrastructureComponent)999,
            null,
            exception));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuntimeInfrastructureFailure(
            RuntimeInfrastructureComponent.Clock,
            0,
            exception));
    }

    [Fact]
    public void Failure_models_are_immutable()
    {
        var types = new[]
        {
            typeof(RuntimeObservabilityFailure),
            typeof(RuntimeObserverFailure),
            typeof(RuntimeInfrastructureFailure)
        };

        Assert.All(types.SelectMany(type => type.GetProperties(
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic)), property => Assert.Null(property.SetMethod));
    }

    private static RuntimeObserverFailure ObserverFailure(long sequence)
    {
        return new RuntimeObserverFailure(
            sequence,
            typeof(OperationReceivedEvent),
            typeof(TestObserver),
            new InvalidOperationException($"Failure {sequence}."));
    }

    private sealed class TestObserver : IRuntimeEventObserver
    {
        public Task ObserveAsync(RuntimeEvent runtimeEvent)
        {
            return Task.CompletedTask;
        }
    }
}
