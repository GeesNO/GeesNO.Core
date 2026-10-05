using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Runtime.Execution;

namespace GeesNO.Core.Tests.Runtime.Execution;

public sealed class MinimumSafetyRulesTests
{
    [Theory]
    [InlineData(ConfirmationPolicy.NotRequired)]
    [InlineData(ConfirmationPolicy.RiskBased)]
    public void Read_only_without_effects_is_allowed(ConfirmationPolicy confirmationPolicy)
    {
        var reference = ExecutionTestData.Reference();
        var descriptor = ExecutionTestData.Descriptor(
            reference,
            confirmationPolicy: confirmationPolicy);
        var support = ExecutionTestData.Support(reference);

        Assert.Null(MinimumSafetyRules.Evaluate(descriptor, support));
    }

    [Fact]
    public void Always_required_is_rejected()
    {
        AssertConfirmationRequired(
            RiskLevel.ReadOnly,
            hasSideEffects: false,
            ConfirmationPolicy.AlwaysRequired);
    }

    [Theory]
    [InlineData(RiskLevel.Low)]
    [InlineData(RiskLevel.High)]
    [InlineData(RiskLevel.Critical)]
    public void Every_non_read_only_risk_is_rejected(RiskLevel riskLevel)
    {
        AssertConfirmationRequired(
            riskLevel,
            hasSideEffects: false,
            ConfirmationPolicy.RiskBased);
    }

    [Fact]
    public void Side_effects_are_rejected_and_undo_does_not_bypass_confirmation()
    {
        var reference = ExecutionTestData.Reference();
        var descriptor = ExecutionTestData.Descriptor(
            reference,
            RiskLevel.Low,
            hasSideEffects: true,
            ConfirmationPolicy.RiskBased);
        var support = ExecutionTestData.Support(
            reference,
            undoSupportLevel: UndoSupportLevel.NativeUndo);

        var error = MinimumSafetyRules.Evaluate(descriptor, support);

        Assert.Equal(RuntimeExecutionErrors.ConfirmationRequiredCode, error?.Code);
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Unsupported)]
    [InlineData(CapabilitySupportLevel.Unavailable)]
    public void Non_executable_support_is_classified_before_safety(
        CapabilitySupportLevel supportLevel)
    {
        var reference = ExecutionTestData.Reference();

        var error = MinimumSafetyRules.Evaluate(
            ExecutionTestData.Descriptor(reference),
            ExecutionTestData.Support(reference, supportLevel));

        Assert.Equal(RuntimeExecutionErrors.CapabilityNotExecutableCode, error?.Code);
    }

    [Fact]
    public async Task Engine_does_not_invoke_tool_when_safety_rejects()
    {
        var reference = ExecutionTestData.Reference();
        var descriptor = ExecutionTestData.Descriptor(
            reference,
            RiskLevel.High,
            hasSideEffects: true,
            ConfirmationPolicy.RiskBased);
        var support = ExecutionTestData.Support(reference);
        var tool = new ExecutionTestTool(reference);
        var engine = ExecutionTestData.CreateEngine(descriptor, support, tool);

        var result = await engine.ExecuteAsync(
            new RuntimeExecutionRequest(ExecutionTestData.Operation(reference)),
            CancellationToken.None);

        Assert.Equal(RuntimeExecutionStatus.Rejected, result.Status);
        Assert.Equal(RuntimeExecutionErrors.ConfirmationRequiredCode, result.Error?.Code);
        Assert.Equal(0, tool.InvocationCount);
        Assert.NotNull(result.ProviderId);
        Assert.Equal(typeof(ExecutionTestResult), result.DeclaredResultType);
    }

    private static void AssertConfirmationRequired(
        RiskLevel riskLevel,
        bool hasSideEffects,
        ConfirmationPolicy confirmationPolicy)
    {
        var reference = ExecutionTestData.Reference();
        var error = MinimumSafetyRules.Evaluate(
            ExecutionTestData.Descriptor(reference, riskLevel, hasSideEffects, confirmationPolicy),
            ExecutionTestData.Support(reference));

        Assert.Equal(RuntimeExecutionErrors.ConfirmationRequiredCode, error?.Code);
    }
}
