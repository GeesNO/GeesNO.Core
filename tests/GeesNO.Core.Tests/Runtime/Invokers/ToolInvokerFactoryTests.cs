using System.Reflection;
using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Primitives;
using GeesNO.Core.Runtime.Invokers;

namespace GeesNO.Core.Tests.Runtime.Invokers;

public sealed class ToolInvokerFactoryTests
{
    [Fact]
    public void Single_closed_contract_is_accepted_with_exact_types()
    {
        var key = InvokerTestData.Key();

        var invoker = ToolInvokerFactory.Create(key, new RecordingTool(key.Capability));

        Assert.Equal(key, invoker.Key);
        Assert.Equal(typeof(InvocationTestOperation), invoker.OperationType);
        Assert.Equal(typeof(InvocationTestResult), invoker.ResultType);
    }

    [Fact]
    public void Closed_contract_inherited_from_base_class_is_accepted()
    {
        var key = InvokerTestData.Key();

        var invoker = ToolInvokerFactory.Create(key, new InheritedTool(key.Capability));

        Assert.Equal(typeof(InvocationTestOperation), invoker.OperationType);
        Assert.Equal(typeof(InvocationTestResult), invoker.ResultType);
    }

    [Fact]
    public void Redundant_identical_inherited_contract_is_deduplicated()
    {
        var key = InvokerTestData.Key();

        var invoker = ToolInvokerFactory.Create(key, new RedundantInheritedTool(key.Capability));

        Assert.Equal(typeof(InvocationTestOperation), invoker.OperationType);
    }

    [Fact]
    public void Base_only_tool_is_rejected_with_diagnostic_code()
    {
        var key = InvokerTestData.Key();

        var exception = Assert.Throws<ArgumentException>(() =>
            ToolInvokerFactory.Create(key, new BaseOnlyTool(key.Capability)));

        Assert.Contains(RuntimeInvocationErrors.ToolContractMismatchCode, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Multiple_closed_contracts_are_rejected_even_for_the_same_capability()
    {
        var key = InvokerTestData.Key();

        var exception = Assert.Throws<ArgumentException>(() =>
            ToolInvokerFactory.Create(key, new MultipleContractTool(key.Capability)));

        Assert.Contains(RuntimeInvocationErrors.ToolContractMismatchCode, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Interface_operation_contract_is_rejected()
    {
        var key = InvokerTestData.Key();

        var exception = Assert.Throws<ArgumentException>(() =>
            ToolInvokerFactory.Create(key, new InterfaceOperationTool(key.Capability)));

        Assert.Contains(RuntimeInvocationErrors.ToolContractMismatchCode, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Abstract_operation_contract_is_rejected()
    {
        var key = InvokerTestData.Key();

        var exception = Assert.Throws<ArgumentException>(() =>
            ToolInvokerFactory.Create(key, new AbstractOperationTool(key.Capability)));

        Assert.Contains(RuntimeInvocationErrors.ToolContractMismatchCode, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fatal_exception_during_reflective_creation_is_not_converted_to_mismatch()
    {
        var key = InvokerTestData.Key();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            ToolInvokerFactory.Create(key, new FatalCapabilityTool()));

        Assert.IsType<OutOfMemoryException>(exception.InnerException);
        Assert.DoesNotContain(
            RuntimeInvocationErrors.ToolContractMismatchCode,
            exception.Message,
            StringComparison.Ordinal);
    }

    private sealed class InheritedTool : RecordingTool
    {
        internal InheritedTool(CapabilityReference capability)
            : base(capability)
        {
        }
    }

    private sealed class RedundantInheritedTool : RecordingTool,
        ITool<InvocationTestOperation, InvocationTestResult>
    {
        internal RedundantInheritedTool(CapabilityReference capability)
            : base(capability)
        {
        }
    }

    private sealed class BaseOnlyTool : ITool
    {
        internal BaseOnlyTool(CapabilityReference capability)
        {
            Capability = capability;
        }

        public CapabilityReference Capability { get; }
    }

    private sealed class FatalCapabilityTool : ITool<InvocationTestOperation, InvocationTestResult>
    {
        public CapabilityReference Capability => throw new OutOfMemoryException("fatal");

        public Task<Result<InvocationTestResult>> ExecuteAsync(
            InvocationTestOperation operation,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The tool must not execute during registration.");
        }
    }

    private sealed class MultipleContractTool :
        ITool<InvocationTestOperation, InvocationTestResult>,
        ITool<SecondOperation, SecondResult>
    {
        internal MultipleContractTool(CapabilityReference capability)
        {
            Capability = capability;
        }

        public CapabilityReference Capability { get; }

        public Task<Result<InvocationTestResult>> ExecuteAsync(
            InvocationTestOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<InvocationTestResult>.Success(new InvocationTestResult("first")));
        }

        public Task<Result<SecondResult>> ExecuteAsync(
            SecondOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<SecondResult>.Success(new SecondResult("second")));
        }
    }

    private interface IInterfaceOperation : IOperation<InvocationTestResult>
    {
    }

    private sealed class InterfaceOperationTool : ITool<IInterfaceOperation, InvocationTestResult>
    {
        internal InterfaceOperationTool(CapabilityReference capability)
        {
            Capability = capability;
        }

        public CapabilityReference Capability { get; }

        public Task<Result<InvocationTestResult>> ExecuteAsync(
            IInterfaceOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<InvocationTestResult>.Success(new InvocationTestResult("ok")));
        }
    }

    private abstract class AbstractOperation : IOperation<InvocationTestResult>
    {
        public abstract OperationId Id { get; }

        public abstract CapabilityReference Capability { get; }
    }

    private sealed class AbstractOperationTool : ITool<AbstractOperation, InvocationTestResult>
    {
        internal AbstractOperationTool(CapabilityReference capability)
        {
            Capability = capability;
        }

        public CapabilityReference Capability { get; }

        public Task<Result<InvocationTestResult>> ExecuteAsync(
            AbstractOperation operation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<InvocationTestResult>.Success(new InvocationTestResult("ok")));
        }
    }

    private sealed record SecondResult(string Value);

    private sealed record SecondOperation(
        OperationId Id,
        CapabilityReference Capability) : IOperation<SecondResult>;
}
