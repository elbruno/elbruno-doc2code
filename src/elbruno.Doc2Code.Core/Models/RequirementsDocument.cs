// elbruno.Doc2Code — parsed input document carrying raw text, structured sections, and file-level metadata.
namespace elbruno.Doc2Code.Core.Models;

/// <summary>Represents a requirements document after initial parsing.</summary>
public sealed class RequirementsDocument
{
    public required string SourceFileName { get; init; }
    public string ExtractedText { get; set; } = "";
    public List<DocSegment> Segments { get; init; } = [];
    public Dictionary<string, string> Tags { get; init; } = [];

    /// <summary>Convenience: total character count of the extracted text.</summary>
    public int TextLength => ExtractedText.Length;
}

/// <summary>A logical segment inside the parsed document (heading + body).</summary>
public sealed class DocSegment
{
    public required string Heading { get; init; }
    public string Body { get; set; } = "";
    public int SortIndex { get; init; }
}
