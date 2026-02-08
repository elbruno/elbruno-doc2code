// elbruno.Doc2Code — centralised timeout constants for the entire solution.
// All timeout values should be defined here so there is a single entry point
// for tuning request/resilience timeouts across every service.
namespace elbruno.Doc2Code.ServiceDefaults;

/// <summary>
/// Single source of truth for every timeout used across the Doc2Code services.
/// Change values here and they automatically propagate to resilience handlers,
/// manually-created <see cref="System.Net.Http.HttpClient"/> instances, and
/// any other component that references this class.
/// </summary>
public static class Doc2CodeTimeouts
{
    /// <summary>
    /// Per-attempt timeout for an individual HTTP request.
    /// Used by the standard resilience handler and by manually-created HttpClients
    /// (e.g. the Ollama LLM client).
    /// </summary>
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Sampling window for the circuit-breaker. Should be ≥ 2× <see cref="AttemptTimeout"/>
    /// so the breaker can observe enough data before tripping.
    /// </summary>
    public static readonly TimeSpan CircuitBreakerSamplingDuration = AttemptTimeout * 2;

    /// <summary>
    /// Absolute upper bound for a request including all retry attempts.
    /// Should be ≥ 3× <see cref="AttemptTimeout"/> to allow for retries.
    /// </summary>
    public static readonly TimeSpan TotalRequestTimeout = AttemptTimeout * 3;

    /// <summary>
    /// Maximum number of retry attempts before giving up.
    /// </summary>
    public const int MaxRetryAttempts = 1;

    /// <summary>
    /// Timeout applied to the raw <see cref="System.Net.Http.HttpClient"/>
    /// used by OllamaSharp for LLM inference calls. Matches <see cref="AttemptTimeout"/>
    /// so manually-created clients stay consistent with resilience-handled ones.
    /// </summary>
    public static readonly TimeSpan LlmHttpClientTimeout = AttemptTimeout;
}
