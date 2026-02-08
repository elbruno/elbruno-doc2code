# Agent System Deep Dive

## Architecture

Every agent in the pipeline implements `IAgent<TIn, TOut>` and inherits from
`LlmAgentBase<TIn, TOut>`. The base class handles:

1. Composing a `ChatMessage` list (system instruction + user message)
2. Calling the LLM via `IChatClient.GetResponseAsync`
3. Stripping markdown code fences from the response
4. Deserialising the JSON into `TOut`
5. Emitting `AgentLogEntry` messages through the `IProgress<T>` callback

### Subclass contract

| Override | Purpose |
|----------|---------|
| `DisplayName` | Label shown in logs and the Aspire dashboard |
| `SystemInstruction` | The system prompt sent to the LLM — defines persona and output schema |
| `ComposeUserMessage(TIn)` | Builds the user-message string from the typed input |
| `ParseResponse(string)` | Deserialises the cleaned JSON into `TOut` |

## Pipeline Order

```
Analyst  →  Architect  →  Developer  ⇄  Reviewer  →  Testing  →  Documentation
                              ↑ retry loop (max 2) ↵
```

`AgentOrchestrator` drives the sequence. If the Reviewer scores below 70 the
Developer is re-invoked with the same inputs, up to `PipelineContext.ReviewRetryLimit`
times.

## Agent Prompts

All prompt text lives in `elbruno.Doc2Code.Core.Prompts.AgentInstructions`.
Each prompt:

- Defines the agent's persona (e.g. "You are a senior .NET architect")
- Specifies the exact JSON schema the model must return
- Lists constraints (e.g. "Do not include markdown fences around JSON")

## Tool Integration

When `SupportsTools` is `true` (default for all agents), the pipeline injects
enabled `AITool` instances into `ChatOptions.Tools`. If the LLM returns a
`FunctionCallContent` the base class invokes the function and feeds the result
back in a multi-turn loop.

The three Microsoft Learn MCP tools are enabled by default because they require
no setup and provide immediate value (official .NET documentation lookup,
code sample search, full page fetch).

## Adding a New Agent

1. Create a new class inheriting `LlmAgentBase<TIn, TOut>` in the Agents project
2. Define the four required overrides
3. Add a prompt constant in `AgentInstructions` (in the Core project)
4. Register the agent as a singleton in `Doc2CodeStartup`
5. Insert it into the `AgentOrchestrator` at the appropriate position
6. (Optional) Create a model class in Core for the agent's output type

## Observability

Every `RunAsync` call creates a tracing span via
`Doc2CodeObservability.StartAgentSpan(DisplayName, "run")`. These spans appear
in the Aspire dashboard under the `elbruno.Doc2Code.AgentPipeline` activity source.
