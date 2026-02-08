namespace elbruno.Doc2Code.DocumentProcessing.Tests;

using System.Text;
using elbruno.Doc2Code.Core.Models;
using FluentAssertions;

public class TextDocumentIngesterTests
{
    private readonly TextDocumentIngester _ingester = new();

    [Theory]
    [InlineData("readme.txt", true)]
    [InlineData("spec.md", true)]
    [InlineData("notes.markdown", true)]
    [InlineData("report.pdf", false)]
    [InlineData("data.docx", false)]
    public void Supports_MatchesExpectedExtensions(string fileName, bool expected)
    {
        _ingester.Supports(fileName).Should().Be(expected);
    }

    [Fact]
    public async Task IngestAsync_PlainText_ReturnsSingleSegment()
    {
        var text = "This is a plain text document without headings.";
        using var stream = ToStream(text);

        var result = await _ingester.IngestAsync(stream, "plain.txt");

        result.SourceFileName.Should().Be("plain.txt");
        result.ExtractedText.Should().Be(text);
        result.Segments.Should().HaveCount(1);
        result.Segments[0].Heading.Should().Be("Full Document");
    }

    [Fact]
    public async Task IngestAsync_MarkdownHeadings_SplitsIntoSegments()
    {
        var md = """
                 # Introduction
                 Some intro text here.

                 ## Requirements
                 Requirement details go here.

                 ## Actors
                 Actor descriptions.
                 """;
        using var stream = ToStream(md);

        var result = await _ingester.IngestAsync(stream, "spec.md");

        result.Segments.Should().HaveCount(3);
        result.Segments[0].Heading.Should().Be("Introduction");
        result.Segments[1].Heading.Should().Be("Requirements");
        result.Segments[2].Heading.Should().Be("Actors");
        result.Segments[0].Body.Should().Contain("intro text");
    }

    [Fact]
    public async Task IngestAsync_SetsIngesterTag()
    {
        using var stream = ToStream("anything");
        var result = await _ingester.IngestAsync(stream, "f.txt");

        result.Tags.Should().ContainKey("ingester");
        result.Tags["ingester"].Should().Be(nameof(TextDocumentIngester));
    }

    private static MemoryStream ToStream(string content)
        => new(Encoding.UTF8.GetBytes(content));
}

