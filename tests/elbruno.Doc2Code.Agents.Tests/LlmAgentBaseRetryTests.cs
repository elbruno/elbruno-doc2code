namespace elbruno.Doc2Code.Agents.Tests;

using System.Text.Json;
using elbruno.Doc2Code.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.AI;

public class LlmAgentBaseRetryTests
{
    /// <summary>
    /// A concrete agent subclass for testing, producing a simple JSON model.
    /// </summary>
    private sealed class TestAgent : LlmAgentBase<string, AnalysisResult>
    {
        public TestAgent(IChatClient chat) : base(chat) { }
        public override string DisplayName => "TestAgent";
        protected override string SystemInstruction => "Return valid JSON.";
        protected override string ComposeUserMessage(string payload) => payload;
        protected override AnalysisResult ParseResponse(string json) => Deserialize<AnalysisResult>(json);
    }

    /// <summary>
    /// A fake IChatClient that returns a sequence of responses across calls.
    /// </summary>
    private sealed class SequentialChatClient : IChatClient
    {
        private readonly Queue<string> _responses;

        public SequentialChatClient(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var text = _responses.Count > 0 ? _responses.Dequeue() : "{}";
            yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            await Task.CompletedTask;
        }
    }

    private static readonly string ValidAnalysisJson = JsonSerializer.Serialize(new
    {
        domainEntities = Array.Empty<object>(),
        systemActors = Array.Empty<object>(),
        domainRules = Array.Empty<object>(),
        stateFlows = Array.Empty<object>(),
        overviewNotes = "test"
    });

    [Fact]
    public async Task RunAsync_ValidJsonOnFirstCall_Succeeds()
    {
        var client = new SequentialChatClient(ValidAnalysisJson);
        var agent = new TestAgent(client);
        var log = new Progress<AgentLogEntry>();

        var result = await agent.RunAsync("test input", log);

        result.Should().NotBeNull();
        result.OverviewNotes.Should().Be("test");
    }

    [Fact]
    public async Task RunAsync_InvalidJsonThenValid_SucceedsOnRetry()
    {
        // First call returns invalid JSON, second call (retry) returns valid JSON
        var client = new SequentialChatClient(
            "{ invalid json !!!",
            ValidAnalysisJson);
        var agent = new TestAgent(client);
        var log = new Progress<AgentLogEntry>();

        var result = await agent.RunAsync("test input", log);

        result.Should().NotBeNull();
        result.OverviewNotes.Should().Be("test");
    }

    [Fact]
    public async Task RunAsync_RepairableJson_SucceedsWithoutRetry()
    {
        // JSON with a trailing comma — repairable without LLM call
        var repairable = ValidAnalysisJson.TrimEnd('}') + ",}";
        var client = new SequentialChatClient(repairable);
        var agent = new TestAgent(client);
        var logs = new List<AgentLogEntry>();
        var log = new Progress<AgentLogEntry>(entry => logs.Add(entry));

        var result = await agent.RunAsync("test input", log);

        result.Should().NotBeNull();
        // Verify repair was logged
        logs.Should().Contain(l => l.Text.Contains("JSON repair applied"));
    }

    [Fact]
    public async Task RunAsync_PermanentlyInvalidJson_ThrowsAfterRetries()
    {
        // All responses are invalid — should exhaust retries and throw
        var client = new SequentialChatClient(
            "not json at all",
            "still not json",
            "nope");
        var agent = new TestAgent(client);
        var log = new Progress<AgentLogEntry>();

        var act = async () => await agent.RunAsync("test input", log);

        await act.Should().ThrowAsync<JsonException>()
            .WithMessage("*TestAgent*");
    }
}
