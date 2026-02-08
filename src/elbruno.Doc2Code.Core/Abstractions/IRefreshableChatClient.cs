// elbruno.Doc2Code — interface for chat clients that can be refreshed at runtime.
namespace elbruno.Doc2Code.Core.Abstractions;

/// <summary>
/// Marks an <c>IChatClient</c> implementation that can reload its configuration
/// at runtime (e.g. when the user changes the LLM provider in settings).
/// </summary>
public interface IRefreshableChatClient
{
    Task RefreshAsync(CancellationToken ct = default);
}
