# Roadmap: JSON Fixer Agent (Layer 3 Resilience)

**Date:** 2026-02-08  
**Status:** Future Enhancement  
**Priority:** Low — implement only if Layers 1 and 2 prove insufficient in practice

---

## Summary

Add a dedicated `JsonFixerAgent` as a third resilience layer for handling LLM JSON output failures across all pipeline agents. This agent would be invoked only when the first two layers (instant regex repair and same-agent re-prompt) both fail.

---

## Current Resilience Strategy (Layers 1 & 2)

| Layer | Mechanism | Latency | Cost | Coverage |
|-------|-----------|---------|------|----------|
| **1** | `JsonRepairHelper` — regex-based syntax fixes (missing commas, trailing commas, truncated brackets) | Microseconds | Zero | ~90% of failures |
| **2** | Re-prompt the same agent — send the error back and ask it to fix its own JSON output (up to 2 retries) | 5-15s per retry | Token cost of re-streaming | ~95%+ of remaining failures |

---

## Proposed Layer 3: JSON Fixer Agent

### How It Would Work

A new `JsonFixerAgent` would:

1. Receive the broken JSON string, the `JsonException` error message, and the expected JSON schema description
2. Send a targeted prompt: *"Fix this JSON. The error is: {error}. The expected schema is: {schema}. Return only the corrected, complete JSON."*
3. Return the repaired JSON string
4. Be called from `LlmAgentBase.RunAsync()` only after Layer 1 (repair) and Layer 2 (re-prompt) have both failed

### Architecture

```
Agent RunAsync()
  → ParseResponse() fails with JsonException
    → Layer 1: JsonRepairHelper.TryRepair() — instant regex fix
      → Success? Use repaired result. Done.
      → Fail?
    → Layer 2: Re-prompt same agent with error context (up to 2 retries)
      → Success? Use new result. Done.
      → Fail?
    → Layer 3: JsonFixerAgent.FixAsync(brokenJson, error, schema)
      → Success? Use fixed result. Done.
      → Fail? Pipeline faults with enriched error.
```

### Implementation Notes

- The fixer agent **must NOT** inherit from `LlmAgentBase` (to avoid recursive fixing). Use a simpler, standalone implementation that calls `IChatClient` directly.
- Consider using a **different, smaller/faster model** for the fixer (e.g., a local Ollama model) to reduce latency and avoid hitting the same provider issues.
- The fixer needs schema descriptions per agent output type. Store these as static strings alongside the agent prompts.
- Add a `MaxFixerInputLength` limit — if the broken JSON exceeds the context window, skip Layer 3 and fault directly with a clear error message.

---

## Pros

| # | Benefit |
|---|---------|
| 1 | **Semantic understanding** — can understand the *intent* of broken JSON, not just syntax. Can complete truncated content, not just close brackets. |
| 2 | **Handles regex-impossible cases** — broken escape sequences, mismatched quotes inside code strings, invalid Unicode, mixed quote styles. |
| 3 | **Schema-aware repair** — knows the expected shape and can add missing required fields or restructure malformed output. |
| 4 | **Self-improving** — improves as LLMs improve, without code changes. |
| 5 | **Clean separation of concerns** — last-resort repair is a well-defined agent responsibility. |

## Cons

| # | Drawback |
|---|----------|
| 1 | **Latency** — full LLM round-trip (5-15s) for each fix attempt. |
| 2 | **Token cost** — broken JSON from Developer/Testing agents can be 50-100KB, doubling token consumption. |
| 3 | **The fixer can also fail** — it may itself produce malformed JSON. A final regex repair pass after the fixer mitigates this. |
| 4 | **Context window limits** — very large broken outputs may not fit in the fixer's context window. |
| 5 | **Same-provider risk** — if the LLM provider is rate-limited or unstable, the fixer hits the same issues. Using a different model mitigates this. |
| 6 | **Recursion guard needed** — the fixer must not use `LlmAgentBase` to avoid infinite fixing loops. |

---

## Trigger for Implementation

Implement this feature when:

- Pipeline runs show Layer 2 (re-prompt) failing more than ~5% of the time
- Users report truncated or semantically incomplete outputs that regex repair can't fix
- A use case requires very large outputs (50+ artifacts) where truncation is common

---

## Related Plans

- [plan-2026-02-08_1035.md](../plans/plan-2026-02-08_1035.md) — Layers 1 & 2 implementation (JSON repair + retry)
