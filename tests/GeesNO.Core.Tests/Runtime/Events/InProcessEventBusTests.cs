using System.Reflection;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Observability;
using GeesNO.Core.Tests.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Events;

public sealed class InProcessEventBusTests
{
    [Fact]
    public void Constructor_rejects_null_collection_diagnostics_and_elements()
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();

        Assert.Throws<ArgumentNullException>(() => new InProcessEventBus(null!, diagnostics));
        Assert.Throws<ArgumentNullException>(() => new InProcessEventBus([], null!));
        Assert.Throws<ArgumentException>(() => new InProcessEventBus([null!], diagnostics));
    }

    [Fact]
    public async Task Empty_bus_accepts_an_event()
    {
        var bus = new InProcessEventBus([], new RuntimeObservabilityDiagnostics());

        await bus.PublishAsync(ObservabilityTestData.Received());
    }

    [Fact]
    public async Task Bus_rejects_null_event()
    {
        var bus = new InProcessEventBus([], new RuntimeObservabilityDiagnostics());

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync(null!));
    }

    [Fact]
    public async Task Constructor_copies_observers_and_preserves_order()
    {
        var calls = new List<int>();
        var first = new DelegateObserver(_ =>
        {
            calls.Add(1);
            return Task.CompletedTask;
        });
        var second = new DelegateObserver(_ =>
        {
            calls.Add(2);
            return Task.CompletedTask;
        });
        IRuntimeEventObserver[] observers = [first, second];
        var bus = new InProcessEventBus(observers, new RuntimeObservabilityDiagnostics());
        observers[0] = new DelegateObserver(_ => Task.CompletedTask);

        await bus.PublishAsync(ObservabilityTestData.Received());

        Assert.Equal([1, 2], calls);
    }

    [Fact]
    public async Task Every_observer_receives_the_same_event_instance()
    {
        var received = new List<RuntimeEvent>();
        var bus = new InProcessEventBus(
            [
                new DelegateObserver(runtimeEvent =>
                {
                    received.Add(runtimeEvent);
                    return Task.CompletedTask;
                }),
                new DelegateObserver(runtimeEvent =>
                {
                    received.Add(runtimeEvent);
                    return Task.CompletedTask;
                })
            ],
            new RuntimeObservabilityDiagnostics());
        var runtimeEvent = ObservabilityTestData.Received();

        await bus.PublishAsync(runtimeEvent);

        Assert.Equal(2, received.Count);
        Assert.All(received, item => Assert.Same(runtimeEvent, item));
    }

    [Fact]
    public async Task Synchronous_failure_is_isolated_and_next_observer_runs()
    {
        await AssertOrdinaryFailureIsIsolated(_ => throw new InvalidOperationException("sync"));
    }

    [Fact]
    public async Task Null_task_is_isolated_and_next_observer_runs()
    {
        var failure = await AssertOrdinaryFailureIsIsolated(_ => null!);
        var exception = Assert.IsType<InvalidOperationException>(failure.Exception);

        Assert.Equal("The runtime event observer returned a null task.", exception.Message);
    }

    [Fact]
    public async Task Asynchronous_failure_is_isolated_and_next_observer_runs()
    {
        await AssertOrdinaryFailureIsIsolated(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("async");
        });
    }

    [Fact]
    public async Task Observer_cancellation_is_an_ordinary_observer_failure()
    {
        await AssertOrdinaryFailureIsIsolated(_ => Task.FromCanceled(new CancellationToken(true)));
    }

    [Fact]
    public async Task Fatal_observer_failure_escapes_immediately()
    {
        var laterCalled = false;
        var bus = new InProcessEventBus(
            [
                new DelegateObserver(_ => throw new OutOfMemoryException("fatal")),
                new DelegateObserver(_ =>
                {
                    laterCalled = true;
                    return Task.CompletedTask;
                })
            ],
            new RuntimeObservabilityDiagnostics());

        await Assert.ThrowsAsync<OutOfMemoryException>(() =>
            bus.PublishAsync(ObservabilityTestData.Received()));
        Assert.False(laterCalled);
    }

    [Fact]
    public async Task Different_publications_can_enter_an_observer_concurrently()
    {
        var entered = 0;
        var bothEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new DelegateObserver(_ =>
        {
            if (Interlocked.Increment(ref entered) == 2)
            {
                bothEntered.TrySetResult(true);
            }

            return release.Task;
        });
        var bus = new InProcessEventBus([observer], new RuntimeObservabilityDiagnostics());

        var first = bus.PublishAsync(ObservabilityTestData.Received(1));
        var second = bus.PublishAsync(ObservabilityTestData.Received(2));
        await bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        release.SetResult(true);
        await Task.WhenAll(first, second);

        Assert.Equal(2, entered);
    }

    [Fact]
    public async Task Concurrent_publications_do_not_guarantee_numeric_completion_order()
    {
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionOrder = new List<long>();
        var observer = new DelegateObserver(async runtimeEvent =>
        {
            if (runtimeEvent.Sequence == 1)
            {
                await releaseFirst.Task;
            }

            lock (completionOrder)
            {
                completionOrder.Add(runtimeEvent.Sequence);
            }
        });
        var bus = new InProcessEventBus([observer], new RuntimeObservabilityDiagnostics());

        var first = bus.PublishAsync(ObservabilityTestData.Received(1));
        var second = bus.PublishAsync(ObservabilityTestData.Received(2));
        await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(first.IsCompleted);
        releaseFirst.SetResult(true);
        await first;

        Assert.Equal([2, 1], completionOrder);
    }

    [Fact]
    public void Bus_has_no_dynamic_registration_or_cancellation_token()
    {
        var methods = typeof(InProcessEventBus).GetMethods(
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly);
        var publish = Assert.Single(methods, method => method.Name == "PublishAsync");

        Assert.DoesNotContain(methods, method =>
            method.Name.Contains("Register", StringComparison.Ordinal)
            || method.Name.Contains("Subscribe", StringComparison.Ordinal)
            || method.Name.Contains("Remove", StringComparison.Ordinal));
        Assert.DoesNotContain(publish.GetParameters(), parameter =>
            parameter.ParameterType == typeof(CancellationToken));
        Assert.DoesNotContain(typeof(IRuntimeEventObserver).GetMethods()
            .SelectMany(method => method.GetParameters()), parameter =>
            parameter.ParameterType == typeof(CancellationToken));
    }

    private static async Task<RuntimeObserverFailure> AssertOrdinaryFailureIsIsolated(
        Func<RuntimeEvent, Task> failingHandler)
    {
        var diagnostics = new RuntimeObservabilityDiagnostics();
        var laterCalled = false;
        var bus = new InProcessEventBus(
            [
                new DelegateObserver(failingHandler),
                new DelegateObserver(_ =>
                {
                    laterCalled = true;
                    return Task.CompletedTask;
                })
            ],
            diagnostics);
        var runtimeEvent = ObservabilityTestData.Received(42);

        await bus.PublishAsync(runtimeEvent);

        Assert.True(laterCalled);
        var failure = Assert.IsType<RuntimeObserverFailure>(Assert.Single(diagnostics.GetFailures()));
        Assert.Equal(42, failure.EventSequence);
        Assert.Equal(typeof(OperationReceivedEvent), failure.EventType);
        Assert.Equal(typeof(DelegateObserver), failure.ObserverType);
        return failure;
    }

    private sealed class DelegateObserver(Func<RuntimeEvent, Task> handler) : IRuntimeEventObserver
    {
        public Task ObserveAsync(RuntimeEvent runtimeEvent)
        {
            return handler(runtimeEvent);
        }
    }
}
