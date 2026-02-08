// elbruno.Doc2Code — HTTP client wrapper for the ApiService generation endpoints.
namespace elbruno.Doc2Code.Web.Services;

using System.Net.Http.Json;
using elbruno.Doc2Code.Core.DTOs;

/// <summary>
/// Provides typed methods for calling the ApiService generation endpoints.
/// </summary>
public sealed class GenerationApiClient
{
    private readonly HttpClient _http;

    public GenerationApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Uploads a requirements document to start generation.</summary>
    public async Task<string> SubmitDocumentAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GenerationStartResult>(ct);
        return result?.RunId ?? throw new InvalidOperationException("No run ID returned.");
    }

    /// <summary>Polls the current status of a generation run.</summary>
    public async Task<GenerationStatus> PollStatusAsync(string runId, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<GenerationStatus>($"/api/generate/{runId}/status", ct)
               ?? new GenerationStatus();
    }

    /// <summary>Downloads the generated archive as a byte array.</summary>
    public async Task<byte[]> DownloadArchiveAsync(string runId, CancellationToken ct = default)
    {
        return await _http.GetByteArrayAsync($"/api/generate/{runId}/download", ct);
    }

    /// <summary>Tests connectivity to the specified LLM provider.</summary>
    public async Task<TestConnectionResult> TestConnectionAsync(TestConnectionRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/settings/test-connection", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TestConnectionResult>(ct)
               ?? new TestConnectionResult { Success = false, Message = "No response from server." };
    }

    /// <summary>Minimal response type from the generation start endpoint.</summary>
    public sealed class GenerationStartResult
    {
        public string? RunId { get; set; }
    }

    /// <summary>Minimal status response from the generation polling endpoint.</summary>
    public sealed class GenerationStatus
    {
        public string? Stage { get; set; }
        public int CompletionPercent { get; set; }
        public string? FailureReason { get; set; }
        public bool IsComplete => Stage is "Done" or "Faulted";
    }
}
