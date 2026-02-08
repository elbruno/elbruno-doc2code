// elbruno.Doc2Code — ingests uploaded documents (PDF/DOCX/TXT) and converts them
// to a structured RequirementsDocument.  Uses plain text extraction as a
// baseline; a MarkItDown integration can be swapped in later.
namespace elbruno.Doc2Code.DocumentProcessing;

using System.Text;
using System.Text.RegularExpressions;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Reads a file stream, extracts its textual content, and splits the text
/// into logical segments based on Markdown-style headings or blank-line
/// separation.  This keeps the implementation self-contained and testable
/// without requiring external native libraries.
/// </summary>
public sealed partial class TextDocumentIngester : IDocumentIngester
{
    // Regex that recognizes Markdown headings (# … ####)
    [GeneratedRegex(@"^#{1,4}\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeadingPattern();

    private static readonly HashSet<string> s_supportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md", ".markdown" };

    /// <inheritdoc />
    public bool Supports(string fileName)
        => s_supportedExtensions.Contains(Path.GetExtension(fileName));

    /// <inheritdoc />
    public async Task<RequirementsDocument> IngestAsync(
        Stream content, string originalName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        var fullText = await reader.ReadToEndAsync(ct);

        var segments = SplitIntoSegments(fullText);

        return new RequirementsDocument
        {
            SourceFileName = originalName,
            ExtractedText = fullText,
            Segments = segments,
            Tags = { ["ingester"] = nameof(TextDocumentIngester) }
        };
    }

    /// <summary>
    /// Splits the raw text by Markdown headings.  When no headings are found
    /// the entire text is returned as a single segment.
    /// </summary>
    private static List<DocSegment> SplitIntoSegments(string text)
    {
        var matches = HeadingPattern().Matches(text);
        if (matches.Count == 0)
        {
            return [new DocSegment { Heading = "Full Document", Body = text.Trim(), SortIndex = 0 }];
        }

        var result = new List<DocSegment>(matches.Count);

        for (var i = 0; i < matches.Count; i++)
        {
            var heading = matches[i].Groups[1].Value.Trim();
            var bodyStart = matches[i].Index + matches[i].Length;
            var bodyEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var body = text[bodyStart..bodyEnd].Trim();

            result.Add(new DocSegment { Heading = heading, Body = body, SortIndex = i });
        }

        return result;
    }
}
