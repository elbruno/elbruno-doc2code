// elbruno.Doc2Code — contract for turning uploaded files into RequirementsDocument instances.
namespace elbruno.Doc2Code.Core.Abstractions;

using elbruno.Doc2Code.Core.Models;

/// <summary>Parses binary document content into a structured <see cref="RequirementsDocument"/>.</summary>
public interface IDocumentIngester
{
    Task<RequirementsDocument> IngestAsync(
        Stream content,
        string originalName,
        CancellationToken ct = default);

    bool Supports(string fileName);
}
