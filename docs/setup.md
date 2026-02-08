# Setup Guide

## Prerequisites

| Requirement | Minimum Version | Verify With |
|-------------|----------------|-------------|
| .NET SDK | 10.0.102 | `dotnet --version` |
| Docker Desktop | Latest stable | `docker --version` |
| Ollama | Latest | `ollama --version` |

## Clone and Build

```bash
git clone https://github.com/elbruno/elbruno-doc2code.git
cd elbruno-doc2code
dotnet restore elbruno.Doc2Code.slnx
dotnet build elbruno.Doc2Code.slnx
```

## Pull a Local Model

The pipeline needs a model available in Ollama. The default is `ministral-3`:

```bash
ollama pull ministral-3
```

See [recommended-tools-and-skills.md](recommended-tools-and-skills.md) for
alternative models if you have more GPU memory.

## Provider-Specific Setup

### Azure AI Inference (FoundryOpenAI)

1. Provision an Azure AI resource and deploy a model
2. Note the endpoint URL, model name, and API key
3. In the Settings UI, select **Azure AI Inference** and fill in the fields

### FoundryLocal

1. Install the FoundryLocal CLI: `dotnet tool install -g FoundryLocal`
2. Pull a model: `foundry model pull phi-3.5-mini`
3. Start the local server: `foundry model run phi-3.5-mini`
4. In the Settings UI, select **Local FoundryLocal** and configure the model alias

### GitHub Copilot

1. Install the Copilot CLI (`npm install -g @github/copilot` or download from GitHub)
2. Authenticate: `copilot auth login`
3. Verify authentication: `copilot auth status`
4. A GitHub Copilot subscription is required
5. In the Settings UI, select **GitHub Copilot** and choose a model (e.g., `gpt-4.1`)

## Run with Aspire

```bash
dotnet run --project src/elbruno.Doc2Code.AppHost
```

The Aspire dashboard opens automatically and shows the four services:
Ollama, SettingsService, ApiService, and Web frontend.

## IDE Setup

### Visual Studio 2022+

Open `elbruno.Doc2Code.slnx`. The Aspire workload should be installed
(`dotnet workload install aspire`).

### VS Code

Install the **C# Dev Kit** and **.NET Aspire** extensions. Open the
repository root folder.

### JetBrains Rider

Open `elbruno.Doc2Code.slnx`. Aspire support is available from Rider 2024.3+.

## Playwright Screenshot Generation

The project includes Playwright-based end-to-end tests that capture screenshots
of the running application. These screenshots are used to build the visual user
manual.

```bash
# Navigate to the e2e test directory
cd tests/e2e

# Install Node.js dependencies (Playwright and helpers)
npm install

# Download the browser binaries that Playwright needs
npx playwright install

# Run the screenshot-capture tests against the live application
npm run screenshots

# Assemble captured screenshots into the user manual document
npm run generate-manual
```

- **`npm install`** — installs Playwright and any helper packages defined in `package.json`.
- **`npx playwright install`** — downloads Chromium, Firefox, and WebKit binaries used by the tests.
- **`npm run screenshots`** — launches the Playwright tests that navigate the UI and save PNG screenshots to the output folder.
- **`npm run generate-manual`** — compiles the saved screenshots into `docs/user-manual.md` with annotated descriptions.

## First Run Checklist

1. Verify `dotnet build elbruno.Doc2Code.slnx` succeeds with zero errors
2. Verify `dotnet test elbruno.Doc2Code.slnx` passes all tests
3. Confirm Ollama is running (`ollama list` should show your pulled model)
4. Start the AppHost and check the Aspire dashboard for four healthy services
5. Open the Web frontend URL from the dashboard and navigate to the Settings page
