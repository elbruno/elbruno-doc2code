// elbruno.Doc2Code — shared API-key authentication middleware and registration helpers.
// Both ApiService and SettingsService reference ServiceDefaults, so placing
// the middleware here avoids duplication.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace elbruno.Doc2Code.ServiceDefaults;

/// <summary>
/// Configuration options for the shared API-key middleware.
/// </summary>
public sealed class ApiKeyAuthOptions
{
    /// <summary>The expected API key value. Empty/null means auth is disabled.</summary>
    public string? Key { get; set; }

    /// <summary>HTTP header used to transmit the key.</summary>
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Middleware that validates the <c>X-Api-Key</c> header (or <c>access_token</c>
/// query parameter for WebSocket upgrades) against a pre-shared key.
/// When no key is configured the middleware is a transparent pass-through,
/// preserving the zero-config local development experience.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyAuthOptions _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeyAuthOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // If no key is configured, bypass validation entirely.
        if (string.IsNullOrEmpty(_options.Key))
        {
            await _next(context);
            return;
        }

        // Skip health-check endpoints.
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/alive"))
        {
            await _next(context);
            return;
        }

        // Try header first, then query parameter (for SignalR WebSocket upgrade).
        var providedKey = context.Request.Headers[ApiKeyAuthOptions.HeaderName].FirstOrDefault()
                          ?? context.Request.Query["access_token"].FirstOrDefault();

        if (!string.Equals(providedKey, _options.Key, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods to register and apply the API-key middleware.
/// </summary>
public static class ApiKeyAuthExtensions
{
    /// <summary>
    /// Reads the API key from configuration and registers <see cref="ApiKeyAuthOptions"/>.
    /// </summary>
    public static TBuilder AddApiKeyAuth<TBuilder>(
        this TBuilder builder,
        string configSection = "ApiKey") where TBuilder : Microsoft.Extensions.Hosting.IHostApplicationBuilder
    {
        var key = builder.Configuration.GetValue<string>(configSection);
        builder.Services.Configure<ApiKeyAuthOptions>(opts => opts.Key = key);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="ApiKeyMiddleware"/> to the request pipeline.
    /// </summary>
    public static WebApplication UseApiKeyAuth(this WebApplication app)
    {
        app.UseMiddleware<ApiKeyMiddleware>();
        return app;
    }
}

/// <summary>
/// Delegating handler that injects the <c>X-Api-Key</c> header into every
/// outgoing HTTP request. Register on typed <see cref="System.Net.Http.HttpClient"/>
/// instances so service-to-service calls carry the key automatically.
/// </summary>
public sealed class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly string? _key;

    public ApiKeyDelegatingHandler(IOptions<ApiKeyAuthOptions> options)
    {
        _key = options.Value.Key;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_key))
        {
            request.Headers.Remove(ApiKeyAuthOptions.HeaderName);
            request.Headers.Add(ApiKeyAuthOptions.HeaderName, _key);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
