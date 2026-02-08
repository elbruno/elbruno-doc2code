// elbruno.Doc2Code — packages a generated solution into a downloadable archive.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>Creates a ZIP archive from the generated code, tests, and documentation.</summary>
public interface IArchiveBuilder
{
    Task<byte[]> BuildArchiveAsync(
        GeneratedSolution code,
        TestSuite? tests = null,
        DocumentationBundle? docs = null,
        CancellationToken ct = default);
}
