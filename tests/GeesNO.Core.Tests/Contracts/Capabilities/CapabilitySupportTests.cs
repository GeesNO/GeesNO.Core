using GeesNO.Core.Contracts.Capabilities;
using GeesNO.Core.Primitives;

namespace GeesNO.Core.Tests.Contracts.Capabilities;

public sealed class CapabilitySupportTests
{
    [Theory]
    [InlineData("limit.size")]
    [InlineData("geometry.vertex-count")]
    [InlineData("document.selection.max-items")]
    public void Restriction_accepts_canonical_code_without_transformation(string code)
    {
        var restriction = new CapabilityRestriction(code, " Maximum size. ");

        Assert.Equal(code, restriction.Code);
        Assert.Equal("Maximum size.", restriction.Description);
        Assert.Equal(restriction, new CapabilityRestriction(code, "Maximum size."));
    }

    [Fact]
    public void Restriction_rejects_null_code_and_missing_description()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new CapabilityRestriction(null!, "Description"));

        Assert.Equal("code", exception.ParamName);
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityRestriction("limit.size", null!));
        Assert.ThrowsAny<ArgumentException>(() => new CapabilityRestriction("limit.size", " "));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("limit")]
    [InlineData(" limit.size")]
    [InlineData("limit.size ")]
    [InlineData("Limit.size")]
    [InlineData("limit_size")]
    [InlineData("limit/size")]
    [InlineData("limit:size")]
    [InlineData("limit.s\u00edze")]
    [InlineData("limit..size")]
    [InlineData("limit.-size")]
    [InlineData("limit.size-")]
    [InlineData("limit.si--ze")]
    public void Restriction_rejects_noncanonical_code(string code)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new CapabilityRestriction(code, "Description"));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Restriction_enforces_code_length_boundary()
    {
        var maximumLengthCode = $"a.{new string('b', 126)}";
        var excessiveLengthCode = $"a.{new string('b', 127)}";

        Assert.Equal(128, maximumLengthCode.Length);
        Assert.Equal(maximumLengthCode, new CapabilityRestriction(maximumLengthCode, "Description").Code);
        Assert.Throws<ArgumentException>(() => new CapabilityRestriction(excessiveLengthCode, "Description"));
    }

    [Fact]
    public void Restriction_identifier_type_does_not_exist()
    {
        Assert.Null(typeof(CapabilityRestriction).Assembly.GetType(
            "GeesNO.Core.Contracts.Capabilities.CapabilityRestrictionId"));
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Native, 0, null)]
    [InlineData(CapabilitySupportLevel.Partial, 1, null)]
    [InlineData(CapabilitySupportLevel.Emulated, 1, null)]
    [InlineData(CapabilitySupportLevel.Unsupported, 0, null)]
    [InlineData(CapabilitySupportLevel.Unavailable, 0, " Dependency missing. ")]
    public void Constructor_accepts_valid_level_states(
        CapabilitySupportLevel level,
        int restrictionCount,
        string? reason)
    {
        var restrictions = Restrictions(restrictionCount);

        var support = new CapabilitySupport(
            Reference(),
            level,
            UndoSupportLevel.None,
            restrictions,
            reason);

        Assert.Equal(level, support.SupportLevel);
        Assert.Equal(UndoSupportLevel.None, support.UndoSupportLevel);
        Assert.Equal(restrictionCount, support.Restrictions.Count);
        Assert.Equal(reason?.Trim(), support.UnavailableReason);
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Native)]
    [InlineData(CapabilitySupportLevel.Partial)]
    [InlineData(CapabilitySupportLevel.Emulated)]
    public void Executable_levels_accept_every_defined_undo_level(CapabilitySupportLevel level)
    {
        var restrictions = Restrictions(level == CapabilitySupportLevel.Native ? 0 : 1);

        foreach (var undoSupportLevel in Enum.GetValues<UndoSupportLevel>())
        {
            var support = new CapabilitySupport(
                Reference(),
                level,
                undoSupportLevel,
                restrictions,
                null);

            Assert.Equal(undoSupportLevel, support.UndoSupportLevel);
        }
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Unsupported)]
    [InlineData(CapabilitySupportLevel.Unavailable)]
    public void Nonexecutable_levels_reject_undo(CapabilitySupportLevel level)
    {
        var reason = level == CapabilitySupportLevel.Unavailable ? "Dependency missing." : null;

        Assert.Throws<ArgumentException>(() => new CapabilitySupport(
            Reference(),
            level,
            UndoSupportLevel.NativeUndo,
            [],
            reason));
    }

    [Theory]
    [InlineData(CapabilitySupportLevel.Native, 1, null)]
    [InlineData(CapabilitySupportLevel.Native, 0, "reason")]
    [InlineData(CapabilitySupportLevel.Partial, 0, null)]
    [InlineData(CapabilitySupportLevel.Partial, 1, "reason")]
    [InlineData(CapabilitySupportLevel.Emulated, 0, null)]
    [InlineData(CapabilitySupportLevel.Emulated, 1, "reason")]
    [InlineData(CapabilitySupportLevel.Unsupported, 1, null)]
    [InlineData(CapabilitySupportLevel.Unsupported, 0, "reason")]
    [InlineData(CapabilitySupportLevel.Unavailable, 1, "reason")]
    [InlineData(CapabilitySupportLevel.Unavailable, 0, null)]
    [InlineData(CapabilitySupportLevel.Unavailable, 0, " ")]
    public void Constructor_rejects_invalid_level_states(
        CapabilitySupportLevel level,
        int restrictionCount,
        string? reason)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CapabilitySupport(
            Reference(),
            level,
            UndoSupportLevel.None,
            Restrictions(restrictionCount),
            reason));
    }

    [Fact]
    public void Constructor_rejects_null_inputs_and_undefined_enums()
    {
        Assert.Throws<ArgumentNullException>(() => new CapabilitySupport(
            null!, CapabilitySupportLevel.Native, UndoSupportLevel.None, [], null));
        Assert.Throws<ArgumentNullException>(() => new CapabilitySupport(
            Reference(), CapabilitySupportLevel.Native, UndoSupportLevel.None, null!, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapabilitySupport(
            Reference(), (CapabilitySupportLevel)99, UndoSupportLevel.None, [], null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapabilitySupport(
            Reference(), CapabilitySupportLevel.Native, (UndoSupportLevel)99, [], null));
    }

    [Fact]
    public void Constructor_rejects_null_elements_and_duplicate_codes_ordinally()
    {
        Assert.Throws<ArgumentException>(() => new CapabilitySupport(
            Reference(),
            CapabilitySupportLevel.Partial,
            UndoSupportLevel.None,
            [null!],
            null));

        Assert.Throws<ArgumentException>(() => new CapabilitySupport(
            Reference(),
            CapabilitySupportLevel.Partial,
            UndoSupportLevel.None,
            [
                new CapabilityRestriction("limit.size", "First"),
                new CapabilityRestriction("limit.size", "Second")
            ],
            null));
    }

    [Fact]
    public void Restrictions_are_defensively_copied_and_exposed_read_only()
    {
        var source = new List<CapabilityRestriction>
        {
            new("limit.size", "Maximum size")
        };
        var support = new CapabilitySupport(
            Reference(),
            CapabilitySupportLevel.Partial,
            UndoSupportLevel.BestEffort,
            source,
            null);

        source.Add(new CapabilityRestriction("limit.count", "Maximum count"));

        Assert.Single(support.Restrictions);
        var exposed = Assert.IsAssignableFrom<IList<CapabilityRestriction>>(support.Restrictions);
        Assert.Throws<NotSupportedException>(() => exposed.Add(
            new CapabilityRestriction("limit.other", "Other")));
    }

    private static IEnumerable<CapabilityRestriction> Restrictions(int count)
    {
        return Enumerable.Range(1, count)
            .Select(index => new CapabilityRestriction($"limit.{index}", $"Limit {index}"));
    }

    private static CapabilityReference Reference()
    {
        return new CapabilityReference(CapabilityId.Parse("system.ping"), new VersionInfo(1, 0, 0));
    }
}
