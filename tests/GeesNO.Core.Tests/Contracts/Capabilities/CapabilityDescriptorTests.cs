using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class CapabilityDescriptorTests
{
    [Fact]
    public void Constructor_exposes_values_and_trims_text()
    {
        var reference = Reference();

        var descriptor = new CapabilityDescriptor(
            reference,
            " Ping ",
            " Checks runtime identity. ",
            RiskLevel.ReadOnly,
            hasSideEffects: false,
            ConfirmationPolicy.NotRequired);

        Assert.Same(reference, descriptor.Reference);
        Assert.Equal("Ping", descriptor.Name);
        Assert.Equal("Checks runtime identity.", descriptor.Description);
        Assert.Equal(RiskLevel.ReadOnly, descriptor.RiskLevel);
        Assert.False(descriptor.HasSideEffects);
        Assert.Equal(ConfirmationPolicy.NotRequired, descriptor.ConfirmationPolicy);
    }

    [Fact]
    public void Constructor_accepts_valid_side_effecting_combinations()
    {
        var low = Create(RiskLevel.Low, true, ConfirmationPolicy.RiskBased);
        var high = Create(RiskLevel.High, true, ConfirmationPolicy.AlwaysRequired);
        var critical = Create(RiskLevel.Critical, true, ConfirmationPolicy.RiskBased);

        Assert.True(low.HasSideEffects);
        Assert.Equal(RiskLevel.High, high.RiskLevel);
        Assert.Equal(RiskLevel.Critical, critical.RiskLevel);
    }

    [Fact]
    public void Constructor_rejects_missing_required_values()
    {
        Assert.Throws<ArgumentNullException>(() => new CapabilityDescriptor(
            null!, "Ping", "Description", RiskLevel.ReadOnly, false, ConfirmationPolicy.NotRequired));
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityDescriptor(
            Reference(), null!, "Description", RiskLevel.ReadOnly, false, ConfirmationPolicy.NotRequired));
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityDescriptor(
            Reference(), " ", "Description", RiskLevel.ReadOnly, false, ConfirmationPolicy.NotRequired));
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityDescriptor(
            Reference(), "Ping", null!, RiskLevel.ReadOnly, false, ConfirmationPolicy.NotRequired));
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityDescriptor(
            Reference(), "Ping", " ", RiskLevel.ReadOnly, false, ConfirmationPolicy.NotRequired));
    }

    [Fact]
    public void Constructor_rejects_undefined_enums()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create((RiskLevel)99, false, ConfirmationPolicy.NotRequired));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(RiskLevel.Low, true, (ConfirmationPolicy)99));
    }

    [Fact]
    public void Constructor_rejects_read_only_with_side_effects()
    {
        Assert.Throws<ArgumentException>(() => Create(
            RiskLevel.ReadOnly,
            true,
            ConfirmationPolicy.NotRequired));
    }

    [Fact]
    public void Constructor_rejects_critical_without_confirmation()
    {
        Assert.Throws<ArgumentException>(() => Create(
            RiskLevel.Critical,
            true,
            ConfirmationPolicy.NotRequired));
    }

    private static CapabilityDescriptor Create(
        RiskLevel riskLevel,
        bool hasSideEffects,
        ConfirmationPolicy confirmationPolicy)
    {
        return new CapabilityDescriptor(
            Reference(),
            "Ping",
            "Description",
            riskLevel,
            hasSideEffects,
            confirmationPolicy);
    }

    private static CapabilityReference Reference()
    {
        return new CapabilityReference(CapabilityId.Parse("system.ping"), new VersionInfo(1, 0, 0));
    }
}
