// elbruno.Doc2Code — DTO sent by the Blazor frontend to kick off code generation.
namespace elbruno.Doc2Code.Core.DTOs;

/// <summary>Payload for POST /api/generate — carries the uploaded file bytes.</summary>
public sealed class StartGenerationRequest
{
    public required byte[] FileBytes { get; init; }
    public required string OriginalFileName { get; init; }
}
