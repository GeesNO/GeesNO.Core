using System.Reflection;
using GeesNO.Core.Contracts.Operations;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Operations;

public sealed class OperationContractTests
{
    [Fact]
    public void OperationId_wraps_an_object_identifier()
    {
        var value = new ObjectId(Guid.Parse("1199c1af-5284-4f65-8c98-7eebac5c2dbd"));

        var operationId = new OperationId(value);

        Assert.Same(value, operationId.Value);
        Assert.Equal(value.ToString(), operationId.ToString());
    }

    [Fact]
    public void OperationId_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => new OperationId(null!));
    }

    [Fact]
    public void OperationId_new_creates_distinct_non_empty_values()
    {
        var first = OperationId.New();
        var second = OperationId.New();

        Assert.NotEqual(Guid.Empty, first.Value.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void OperationId_has_value_equality_and_hashing()
    {
        var objectId = new ObjectId(Guid.Parse("c4350a82-1188-4435-8cde-4ade801f3169"));
        var first = new OperationId(objectId);
        var second = new OperationId(new ObjectId(objectId.Value));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void OperationId_declares_no_conversion_operators()
    {
        Assert.DoesNotContain(
            typeof(OperationId).GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name is "op_Implicit" or "op_Explicit");
    }

    [Fact]
    public void Generic_operation_is_invariant_and_adds_no_methods()
    {
        var resultParameter = Assert.Single(typeof(IOperation<>).GetGenericArguments());

        Assert.Equal(GenericParameterAttributes.None, resultParameter.GenericParameterAttributes);
        Assert.Empty(typeof(IOperation<>).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal([typeof(IOperation)], typeof(IOperation<>).GetInterfaces());
    }
}
