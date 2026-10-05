using System.Reflection;
using GeesNO.Core.Contracts.Tools;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Runtime.Invokers;

internal static class ToolInvokerFactory
{
    internal static IToolInvoker Create(ProviderCapabilityKey key, ITool tool)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(tool);

        var contracts = tool.GetType()
            .GetInterfaces()
            .Where(interfaceType => interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(ITool<,>))
            .Distinct()
            .ToArray();

        if (contracts.Length != 1)
        {
            throw ContractMismatch(
                nameof(tool),
                "A tool must implement exactly one closed ITool<TOperation,TResult> contract.");
        }

        var contract = contracts[0];
        if (contract.ContainsGenericParameters)
        {
            throw ContractMismatch(nameof(tool), "Open generic tool contracts are not supported.");
        }

        var arguments = contract.GetGenericArguments();
        var operationType = arguments[0];
        var resultType = arguments[1];

        if (operationType.ContainsGenericParameters || resultType.ContainsGenericParameters)
        {
            throw ContractMismatch(nameof(tool), "Open generic tool arguments are not supported.");
        }

        if (operationType.IsInterface || operationType.IsAbstract)
        {
            throw ContractMismatch(nameof(tool), "The operation type must be a concrete type.");
        }

        try
        {
            var invokerType = typeof(ToolInvoker<,>).MakeGenericType(operationType, resultType);
            var invoker = Activator.CreateInstance(
                invokerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: [key, tool],
                culture: null);

            return invoker as IToolInvoker
                ?? throw ContractMismatch(nameof(tool), "The closed tool invoker could not be created.");
        }
        catch (Exception exception) when (IsRecoverableFactoryFailure(exception))
        {
            throw ContractMismatch(nameof(tool), "The closed tool invoker could not be created.", exception);
        }
    }

    private static ArgumentException ContractMismatch(
        string parameterName,
        string detail,
        Exception? innerException = null)
    {
        return new ArgumentException(
            $"{RuntimeInvocationErrors.ToolContractMismatchCode}: {detail}",
            parameterName,
            innerException);
    }

    private static bool IsRecoverableFactoryFailure(Exception exception)
    {
        if (RuntimeExceptionPolicy.IsFatal(exception))
        {
            return false;
        }

        return exception switch
        {
            TargetInvocationException { InnerException: { } innerException }
                => !RuntimeExceptionPolicy.IsFatal(innerException),
            ArgumentException or InvalidCastException or MissingMethodException => true,
            TargetInvocationException => true,
            _ => false
        };
    }
}
