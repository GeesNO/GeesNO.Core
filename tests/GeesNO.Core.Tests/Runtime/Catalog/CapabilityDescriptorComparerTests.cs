using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Runtime.Catalog;

namespace GeesNO.Core.Tests.Runtime.Catalog;

public sealed class CapabilityDescriptorComparerTests
{
    [Fact]
    public void Equivalent_distinct_descriptors_compare_equal()
    {
        var reference = RuntimeCatalogTestData.Reference();
        var first = RuntimeCatalogTestData.Descriptor(reference);
        var second = RuntimeCatalogTestData.Descriptor(RuntimeCatalogTestData.Reference());

        Assert.NotSame(first, second);
        Assert.True(CapabilityDescriptorComparer.Instance.Equals(first, second));
        Assert.Equal(
            CapabilityDescriptorComparer.Instance.GetHashCode(first),
            CapabilityDescriptorComparer.Instance.GetHashCode(second));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("risk")]
    [InlineData("effects")]
    [InlineData("confirmation")]
    public void Semantic_differences_compare_unequal(string difference)
    {
        var reference = RuntimeCatalogTestData.Reference();
        var baseline = RuntimeCatalogTestData.Descriptor(
            reference,
            riskLevel: RiskLevel.Low,
            confirmationPolicy: ConfirmationPolicy.RiskBased);
        var candidate = difference switch
        {
            "name" => RuntimeCatalogTestData.Descriptor(
                reference, name: "Other", riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "description" => RuntimeCatalogTestData.Descriptor(
                reference, description: "Other", riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "risk" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.High,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "effects" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.Low, hasSideEffects: true,
                confirmationPolicy: ConfirmationPolicy.RiskBased),
            "confirmation" => RuntimeCatalogTestData.Descriptor(
                reference, riskLevel: RiskLevel.Low,
                confirmationPolicy: ConfirmationPolicy.NotRequired),
            _ => throw new InvalidOperationException()
        };

        Assert.False(CapabilityDescriptorComparer.Instance.Equals(baseline, candidate));
    }
}
