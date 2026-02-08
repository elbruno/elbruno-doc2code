# Contributing to elbruno.Doc2Code

## Code Style

- Target **.NET 10** (`net10.0`), C# 14
- File-scoped namespaces, one type per file
- Every `.cs` file begins with `// elbruno.Doc2Code — <short purpose>`
- Use `sealed class` with `required` init properties for models
- Keep `Program.cs` minimal — put registration logic in `*Startup.cs` extension classes

## Branch Strategy

- `main` is the default branch
- Feature work goes on `feature/<short-name>` branches
- Bug fixes go on `fix/<short-name>` branches
- Open a pull request against `main` for review

## Pull Request Process

1. Ensure `dotnet build elbruno.Doc2Code.slnx` produces zero errors and warnings
2. Ensure `dotnet test elbruno.Doc2Code.slnx` passes all existing tests
3. Add or update tests for any new functionality
4. Update relevant documentation in `docs/` if behaviour changes
5. Keep commits focused — one logical change per commit

## Test Requirements

- Unit tests use **xUnit** with **FluentAssertions** and **NSubstitute**
- Test projects live under `tests/elbruno.Doc2Code.*.Tests/`
- Name test classes `<Feature>Tests` and methods `<Method>_<Scenario>_<ExpectedResult>`
- Tests must not depend on external services (Ollama, network) — mock `IChatClient`

## Adding a New Project

1. Create the folder under `src/elbruno.Doc2Code.<Name>/`
2. Name the csproj `elbruno.Doc2Code.<Name>.csproj`
3. Add it to `elbruno.Doc2Code.slnx`
4. If it needs Aspire orchestration, reference it from the AppHost and update the
   topology in `Program.cs`
