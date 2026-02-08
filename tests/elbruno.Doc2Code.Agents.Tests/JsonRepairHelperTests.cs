namespace elbruno.Doc2Code.Agents.Tests;

using FluentAssertions;

public class JsonRepairHelperTests
{
    [Fact]
    public void ValidJson_PassesThroughUnchanged()
    {
        var json = """{"name":"test","items":[1,2,3]}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be(json);
        fixes.Should().BeEmpty();
    }

    [Fact]
    public void MissingComma_BetweenObjects_IsInserted()
    {
        var json = """[{"a":1} {"b":2}]""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""[{"a":1}, {"b":2}]""");
        fixes.Should().ContainSingle(f => f.Contains("missing comma"));
    }

    [Fact]
    public void MissingComma_BetweenArrays_IsInserted()
    {
        var json = """[[1,2] [3,4]]""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""[[1,2], [3,4]]""");
        fixes.Should().ContainSingle(f => f.Contains("missing comma"));
    }

    [Fact]
    public void TrailingComma_BeforeClosingBrace_IsRemoved()
    {
        var json = """{"items": [1, 2,]}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""{"items": [1, 2]}""");
        fixes.Should().ContainSingle(f => f.Contains("trailing comma"));
    }

    [Fact]
    public void TrailingComma_BeforeClosingBracket_IsRemoved()
    {
        var json = """{"a": 1, "b": 2,}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""{"a": 1, "b": 2}""");
        fixes.Should().ContainSingle(f => f.Contains("trailing comma"));
    }

    [Fact]
    public void DoubleCommas_AreReduced()
    {
        var json = """[1,, 2]""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""[1, 2]""");
        fixes.Should().ContainSingle(f => f.Contains("duplicate comma"));
    }

    [Fact]
    public void TruncatedJson_IsClosed()
    {
        var json = """{"artifacts": [{"path": "a.cs"}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().EndWith("]}");
        fixes.Should().ContainSingle(f => f.Contains("closing character"));
    }

    [Fact]
    public void StringSafety_ContentInsideStringsIsNotModified()
    {
        // The } { inside the string value must NOT get a comma inserted
        var json = """{"sourceText": "} {"}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be(json);
        fixes.Should().BeEmpty();
    }

    [Fact]
    public void StringSafety_EscapedQuotesAreRespected()
    {
        // Escaped quotes inside a string must not break string tracking
        var json = """{"text": "say \"hello\" world"}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be(json);
        fixes.Should().BeEmpty();
    }

    [Fact]
    public void DeeplyNested_WithMixedIssues()
    {
        // Missing comma between nested objects + trailing comma
        var json = """{"data": [{"x": 1} {"y": 2,}]}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        repaired.Should().Be("""{"data": [{"x": 1}, {"y": 2}]}""");
        fixes.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void EmptyInput_ReturnsEmpty()
    {
        var (repaired, fixes) = JsonRepairHelper.TryRepair("");

        repaired.Should().Be("");
        fixes.Should().BeEmpty();
    }

    [Fact]
    public void NullInput_ReturnsNull()
    {
        var (repaired, fixes) = JsonRepairHelper.TryRepair(null!);

        repaired.Should().BeNull();
        fixes.Should().BeEmpty();
    }

    [Fact]
    public void TruncatedJson_MultipleUnclosedLevels()
    {
        var json = """{"a": {"b": [{"c": 1""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        // 4 unclosed: { { [ { → closers: } ] } }
        repaired.Should().EndWith("}]}}");
        fixes.Should().ContainSingle(f => f.Contains("4 closing character"));
    }

    [Fact]
    public void RepairedJson_IsValidJson()
    {
        // A complex malformed JSON that exercises multiple repair paths
        var json = """{"items": [{"a":1} {"b":2}] "extra": true,}""";
        var (repaired, fixes) = JsonRepairHelper.TryRepair(json);

        // Should be parseable
        var act = () => System.Text.Json.JsonDocument.Parse(repaired);
        act.Should().NotThrow();

        // Verify the missing comma between ] and "extra" was inserted
        repaired.Should().Contain("], \"extra\"");
    }
}
