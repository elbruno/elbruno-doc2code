// elbruno.Doc2Code — DTO returned when a generation run is accepted.
namespace elbruno.Doc2Code.Core.DTOs;

/// <summary>Response for POST /api/generate — carries the run identifier.</summary>
public sealed class StartGenerationResponse
{
    public required string RunId { get; init; }
    public string StatusMessage { get; init; } = "Generation started";
}
