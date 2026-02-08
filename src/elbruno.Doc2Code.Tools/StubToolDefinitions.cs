// elbruno.Doc2Code — placeholder tool definitions for future capabilities.
namespace elbruno.Doc2Code.Tools;

using System.ComponentModel;
using Microsoft.Extensions.AI;

/// <summary>
/// Defines stub <see cref="AITool"/> instances for tools that are not yet implemented.
/// Each stub returns a fixed message explaining the tool is unavailable.
/// </summary>
public static class StubToolDefinitions
{
    /// <summary>Creates all stub tool instances.</summary>
    public static IReadOnlyList<AITool> CreateAll() =>
    [
        AIFunctionFactory.Create(WebSearch, "web_search", "Search the web for information relevant to code generation."),
        AIFunctionFactory.Create(FileRead, "file_read", "Read a file from the local file system."),
        AIFunctionFactory.Create(CodeCompileCheck, "code_compile_check", "Compile generated code and report errors."),
        AIFunctionFactory.Create(NuGetSearch, "nuget_search", "Search NuGet for packages matching a query."),
        AIFunctionFactory.Create(RunUnitTests, "run_unit_tests", "Execute unit tests and report results."),
    ];

    [Description("Search the web for information relevant to code generation.")]
    private static string WebSearch(string query)
        => "Tool 'web_search' is not yet implemented. Enable it in Settings once available.";

    [Description("Read a file from the local file system.")]
    private static string FileRead(string path)
        => "Tool 'file_read' is not yet implemented. Enable it in Settings once available.";

    [Description("Compile generated code and report errors.")]
    private static string CodeCompileCheck(string sourceCode)
        => "Tool 'code_compile_check' is not yet implemented. Enable it in Settings once available.";

    [Description("Search NuGet for packages matching a query.")]
    private static string NuGetSearch(string query)
        => "Tool 'nuget_search' is not yet implemented. Enable it in Settings once available.";

    [Description("Execute unit tests and report results.")]
    private static string RunUnitTests(string projectPath)
        => "Tool 'run_unit_tests' is not yet implemented. Enable it in Settings once available.";
}
