# Recommended Tools, Skills & Local Model Suggestions

> Reference document for the elbruno.Doc2Code agent tool system.

## Tool Inventory

The table below lists every tool recognised by the pipeline.
Tools marked **Implemented** are backed by the
[Microsoft Learn MCP Server](https://learn.microsoft.com/en-us/training/support/mcp)
and work out of the box with no configuration.

| Tool Key | Status | Default | Intended Agents | What It Does |
|----------|--------|---------|-----------------|--------------|
| `get_dotnet_docs` | **Implemented** | Enabled | All | Searches official .NET / Azure docs via MCP (`microsoft_docs_search`) |
| `microsoft_docs_fetch` | **Implemented** | Enabled | Developer, Architect | Fetches full doc pages as markdown via MCP |
| `microsoft_code_sample_search` | **Implemented** | Enabled | Developer, Testing | Searches Microsoft code samples via MCP |
| `web_search` | Stub | Disabled | Architect, Developer | General web lookup for NuGet packages, API patterns |
| `file_read` | Stub | Disabled | Developer | Read files from the local filesystem for context |
| `code_compile_check` | Stub | Disabled | Developer, Reviewer | Validate that generated code compiles |
| `nuget_search` | Stub | Disabled | Architect, Developer | Look up correct package names and versions |
| `run_unit_tests` | Stub | Disabled | Testing, Reviewer | Execute xUnit tests against generated output |

### Additional tools worth considering in future iterations

| Tool Idea | Applicable Agents | Purpose |
|-----------|-------------------|---------|
| `semantic_code_search` | Reviewer | Pattern and anti-pattern detection across generated files |
| `architecture_validator` | Architect | Verify solution structure against .NET project conventions |
| `dependency_analyzer` | Architect | Detect circular references and validate project graphs |
| `code_style_checker` | Reviewer | Enforce naming, formatting, and structure rules |

## MCP Server Integrations

The Microsoft Learn MCP Server (`https://learn.microsoft.com/api/mcp`) is the
first remote MCP integration and serves as the reference architecture for future
servers. It exposes three tools via streamable HTTP, requires no authentication,
and is free to use.

### Potential future MCP server integrations

| Server | Use Case |
|--------|----------|
| GitHub MCP Server | Repository operations, automated PR creation |
| Filesystem MCP Server | Reading reference projects on disk |
| .NET CLI MCP Server | Running `dotnet build` / `dotnet test` from the pipeline |

## Local Model Recommendations

These suggestions are embedded in the SettingsService defaults and shown on the
Settings page. The right model depends on available GPU memory.

| Model | Min VRAM | Pull Command | Notes |
|-------|----------|-------------|-------|
| `ministral-3` | 4 GB | `ollama pull ministral-3` | General-purpose default; runs on most hardware |
| `llama3.2` | 4 GB | `ollama pull llama3.2` | Meta Llama 3.2 — compact, fast, strong general reasoning |
| `devstral-small-2` | 8 GB | `ollama pull devstral-small:24b` | Mistral's code-optimised variant; better at structured output |
| `qwen3-coder-next` | 12 GB | `ollama pull qwen3:32b` | Strong multi-language code generation; needs more VRAM |

> **Tip:** Start with `ministral-3` to verify your setup works, then switch to a
> code-focused model once you've confirmed Ollama is running correctly.
