using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Events;
using GeesNO.Core.Runtime.Execution;
using GeesNO.Core.Runtime.Journal;
using GeesNO.Core.Runtime.Observability;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class ToolEngineObservabilityEquivalenceTests
{
    [Theory]
    [InlineData("success")]
    [InlineData("rejected")]
    [InlineData("functional-failure")]
    [InlineData("unexpected-failure")]
    [InlineData("cancelled")]
    public async Task Observability_modes_preserve_result_semantics(string scenario)
    {
        var operation = ExecutionTestData.Operation();
        var expected = await ExecuteScenarioAsync(
            operation,
            scenario,
            RuntimeObservability.Disabled);

        foreach (var observability in CreateActiveModes())
        {
            var actual = await ExecuteScenarioAsync(operation, scenario, observability);
            AssertEquivalent(expected, actual);
        }
    }

    private static IReadOnlyList<RuntimeObservability> CreateActiveModes()
    {
        return
        [
            ToolEngineObservabilityTestData.CreateObservability(
                [],
                new DelegatingJournalSink(_ => { }),
                new RuntimeObservabilityDiagnostics()),
            ToolEngineObservabilityTestData.CreateObservability(
                [],
                new InMemoryExecutionJournal(),
                new RuntimeObservabilityDiagnostics()),
            ToolEngineObservabilityTestData.CreateObservability(
                [new RecordingRuntimeObserver()],
                new DelegatingJournalSink(_ => { }),
                new RuntimeObservabilityDiagnostics())
        ];
    }

    private static async Task<RuntimeExecutionResult> ExecuteScenarioAsync(
        ExecutionTestOperation operation,
        string scenario,
        RuntimeObservability observability)
    {
        var tool = scenario switch
        {
            "functional-failure" => new ExecutionTestTool(
                operation.Capability,
                (_, _) => Task.FromResult(Result<ExecutionTestResult>.Failure(
                    new Error("tool.failed", "Expected failure.")))),
            "unexpected-failure" => new ExecutionTestTool(
                operation.Capability,
                (_, _) => Task.FromException<Result<ExecutionTestResult>>(
                    new InvalidOperationException("Expected exception."))),
            _ => new ExecutionTestTool(operation.Capability)
        };
        var engine = ExecutionTestData.CreateExecutableEngine(tool, observability: observability);
        var providerId = scenario == "rejected"
            ? ProviderId.Parse("provider.missing")
            : null;
        using var source = new CancellationTokenSource();
        if (scenario == "cancelled")
        {
            source.Cancel();
        }

        return await engine.ExecuteAsync(
            new RuntimeExecutionRequest(operation, providerId),
            source.Token);
    }

    private static void AssertEquivalent(
        RuntimeExecutionResult expected,
        RuntimeExecutionResult actual)
    {
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.OperationId, actual.OperationId);
        Assert.Equal(expected.Capability, actual.Capability);
        Assert.Equal(expected.ProviderId, actual.ProviderId);
        Assert.Equal(expected.DeclaredResultType, actual.DeclaredResultType);
        Assert.Equal(expected.Error, actual.Error);
        Assert.Equal(expected.SuccessfulValue, actual.SuccessfulValue);

        if (expected.InternalException is null)
        {
            Assert.Null(actual.InternalException);
            return;
        }

        Assert.NotNull(actual.InternalException);
        Assert.Equal(expected.InternalException.GetType(), actual.InternalException.GetType());
        Assert.Equal(expected.InternalException.Message, actual.InternalException.Message);
    }
}
