// elbruno.Doc2Code — packages generated artifacts into a ZIP for download.
namespace elbruno.Doc2Code.ApiService.Services;

using System.IO.Compression;
using System.Text;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Flattens all generated artifacts into a single ZIP archive held in memory.
/// Uses a record struct manifest to collect entries before writing.
/// </summary>
public sealed class ZipArchiveBuilder : IArchiveBuilder
{
    private readonly record struct ZipItem(string EntryName, string Text);

    public Task<byte[]> BuildArchiveAsync(
        GeneratedSolution code,
        TestSuite? tests = null,
        DocumentationBundle? docs = null,
        CancellationToken ct = default)
    {
        var root = code.SolutionName;

        // Build a flat manifest of all items to archive
        var manifest = code.Artifacts
            .Select(a => new ZipItem($"{root}/src/{a.Path}", a.SourceText))
            .ToList();

        if (tests is not null)
            manifest.AddRange(tests.TestArtifacts.Select(
                t => new ZipItem($"{root}/tests/{t.Path}", t.SourceText)));

        if (docs is not null)
        {
            manifest.Add(new ZipItem($"{root}/{docs.ReadmeFile.Path}", docs.ReadmeFile.SourceText));
            manifest.Add(new ZipItem($"{root}/{docs.ArchGuide.Path}", docs.ArchGuide.SourceText));
            manifest.AddRange(docs.ExtraDocs.Select(
                d => new ZipItem($"{root}/docs/{d.Path}", d.SourceText)));
        }

        var buf = new MemoryStream();
        using (var zip = new ZipArchive(buf, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var item in manifest)
            {
                ct.ThrowIfCancellationRequested();
                using var w = new StreamWriter(
                    zip.CreateEntry(item.EntryName, CompressionLevel.Fastest).Open(),
                    Encoding.UTF8);
                w.Write(item.Text);
            }

        return Task.FromResult(buf.ToArray());
    }
}
