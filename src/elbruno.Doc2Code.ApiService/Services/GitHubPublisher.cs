// elbruno.Doc2Code — stub for publishing generated code to GitHub.
// The Octokit interaction is intentionally left as a TODO so that
// secrets are never embedded and the build stays clean without
// needing live GitHub access.
namespace elbruno.Doc2Code.ApiService.Services;

using elbruno.Doc2Code.Core.Models;

/// <summary>
/// Placeholder publisher.  When a valid GitHub token is supplied
/// through the Settings page, the full Octokit-based workflow
/// (create repo → build tree → commit) will be implemented here.
/// </summary>
public sealed class GitHubPublisher
{
    public Task<string> PublishSolutionAsync(
        GeneratedSolution solution,
        TestSuite? tests,
        DocumentationBundle? docs,
        string accessToken,
        string? orgName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException(
                "Configure a GitHub token in Settings before publishing.");

        // TODO: wire Octokit calls to create the repo and push artifacts.
        var placeholderUrl = $"https://github.com/{orgName ?? "user"}/{solution.SolutionName}";
        return Task.FromResult(placeholderUrl);
    }
}
