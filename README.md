# elbruno.Doc2Code — Multi-Agent .NET Code Generator

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Aspire](https://img.shields.io/badge/.NET-Aspire-512BD4)](https://learn.microsoft.com/dotnet/aspire/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Turn a natural-language requirements document into a complete .NET solution —
source code, unit tests, and project documentation — using a pipeline of six
specialised AI agents orchestrated by .NET Aspire.

---

## How It Works

```
Requirements Document (.txt)
  │
  ▼
┌──────────┐   ┌───────────┐   ┌───────────┐   ┌──────────┐
│ Analyst  │──▶│ Architect │──▶│ Developer │◀─▶│ Reviewer │
└──────────┘   └───────────┘   └───────────┘   └──────────┘
                                                     │
                                        score ≥ 70?  │ max 2 retries
                                                     ▼
                                               ┌──────────┐   ┌────────────────┐
                                               │ Testing  │──▶│ Documentation  │
                                               └──────────┘   └────────────────┘
                                                                      │
                                                                      ▼
                                                              .NET Solution (.zip)
```

## Service Topology

Four services run under .NET Aspire:

| Service | Role |
|---------|------|
| **Ollama** | Local LLM inference (default model: `ministral-3`) |
| **SettingsService** | Persists configuration to a JSON file |
| **ApiService** | Hosts the agent pipeline, SignalR logs, file upload/download |
| **Web** | Blazor + Bootstrap 5 frontend with dark terminal theme |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.102+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Ollama](https://ollama.com/) (or let Aspire run it in a container)

## Quick Start

```bash
git clone https://github.com/elbruno/elbruno-doc2code.git
cd elbruno-doc2code

# Pull a model (if Ollama is installed locally)
ollama pull ministral-3

# Launch all four services via Aspire
dotnet run --project src/elbruno.Doc2Code.AppHost
```

The Aspire dashboard opens automatically. Click the **Web** endpoint to access
the application, then upload one of the files from the `samples/` directory.

## Project Structure

```
src/
  elbruno.Doc2Code.Core/              # Models, DTOs, abstractions, agent prompts
  elbruno.Doc2Code.LlmProviders/      # LLM provider factory (Ollama, FoundryLocal, Azure AI, Copilot)
  elbruno.Doc2Code.Tools/             # Tool registry, MCP client, stub tools
  elbruno.Doc2Code.Agents/            # Six pipeline agents + orchestrator
  elbruno.Doc2Code.ApiService/        # REST API + SignalR hub
  elbruno.Doc2Code.SettingsService/   # Configuration persistence microservice
  elbruno.Doc2Code.DocumentProcessing/# Document ingestion
  elbruno.Doc2Code.ServiceDefaults/   # Observability helpers
  elbruno.Doc2Code.AppHost/           # Aspire orchestrator
  elbruno.Doc2Code.Web/               # Blazor frontend
tests/
  elbruno.Doc2Code.Core.Tests/
  elbruno.Doc2Code.Agents.Tests/
  elbruno.Doc2Code.DocumentProcessing.Tests/
samples/                               # Ready-to-use requirements documents
docs/                                  # Architecture, setup, configuration guides
```

## Sample Files

Five sample requirements documents are included for quick testing:

| File | Language | Domain |
|------|----------|--------|
| `sample-contact-book.txt` | English | Contact book (add, list, search, delete contacts) |
| `sample-inventory-system.txt` | Spanish | Inventory (products, stock, purchase orders) |
| `sample-library-management.txt` | Spanish | Library (books, readers, loans) |
| `sample-libro-recetas.txt` | Spanish | Recipe book (add, list, search, delete recipes) |
| `sample-task-tracker.txt` | English | Task tracking (projects, tasks, members) |

See [docs/samples.md](docs/samples.md) for details on format and how to write your own.

## Documentation

| Document | Description |
|----------|-------------|
| [docs/setup.md](docs/setup.md) | Prerequisites, cloning, first run |
| [docs/configuration.md](docs/configuration.md) | Full `Doc2CodeConfig` reference, REST endpoints |
| [docs/agents.md](docs/agents.md) | Agent system internals, extending the pipeline |
| [docs/architecture.md](docs/architecture.md) | Overall architecture and design decisions |
| [docs/security.md](docs/security.md) | Token handling, known gaps, mitigations |
| [docs/recommended-tools-and-skills.md](docs/recommended-tools-and-skills.md) | Tool inventory and model recommendations |
| [docs/contributing.md](docs/contributing.md) | Code style, PR process, test requirements |
| [docs/user-manual.md](docs/user-manual.md) | Visual walkthrough of the application UI |

## Screenshots & User Manual

The Web frontend uses a dark terminal/hacker aesthetic with green-on-black
styling, an auto-scrolling console log panel, and a real-time pipeline viewer.
For a visual walkthrough of every screen, see
[docs/user-manual.md](docs/user-manual.md).

## Running Tests

```bash
dotnet test elbruno.Doc2Code.slnx
```

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.

---

Built by [Bruno Capuano](https://github.com/elbruno)
