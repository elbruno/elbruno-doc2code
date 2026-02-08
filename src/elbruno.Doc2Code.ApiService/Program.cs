// elbruno.Doc2Code ApiService entry point.
// All registrations and route mappings live in Doc2CodeStartup.cs.

using elbruno.Doc2Code.Agents;
using elbruno.Doc2Code.Tools;
using elbruno.Doc2Code.ApiService;
using elbruno.Doc2Code.ApiService.Handlers;
using elbruno.Doc2Code.ApiService.Services;
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.DocumentProcessing;
using Microsoft.Extensions.AI;
using elbruno.Doc2Code.LlmProviders;
using elbruno.Doc2Code.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApiKeyAuth();

builder.Services.AddSignalR();

builder.Services.AddTransient<ApiKeyDelegatingHandler>();
builder.Services.AddScoped<RemoteSettingsClient>();
builder.Services.AddHttpClient<RemoteSettingsClient>(c =>
    c.BaseAddress = new Uri("https+http://settingsservice"))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
builder.Services.AddSingleton<ISettingsStore>(sp => sp.GetRequiredService<RemoteSettingsClient>());

// LLM client provider — reads settings from SettingsService and creates the appropriate IChatClient
builder.Services.AddSingleton<ChatClientProvider>();
builder.Services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<ChatClientProvider>());

// Tool system — MCP client + registry with lazy initialization
builder.Services.AddSingleton<MicrosoftLearnMcpClient>();
builder.Services.AddSingleton<ToolRegistry>();

// Pipeline agents — each wraps IChatClient with a domain-specific prompt
builder.Services.AddSingleton<AnalystAgent>();
builder.Services.AddSingleton<ArchitectAgent>();
builder.Services.AddSingleton<DeveloperAgent>();
builder.Services.AddSingleton<ReviewerAgent>();
builder.Services.AddSingleton<TestingAgent>();
builder.Services.AddSingleton<DocumentationAgent>();
builder.Services.AddSingleton<AgentOrchestrator>();
builder.Services.AddSingleton<IGenerationPipeline>(sp => sp.GetRequiredService<AgentOrchestrator>());
// Supporting services
builder.Services.AddSingleton<IDocumentIngester, TextDocumentIngester>();
builder.Services.AddSingleton<IArchiveBuilder, ZipArchiveBuilder>();
builder.Services.AddSingleton<RunTracker>();
builder.Services.AddSingleton<GitHubPublisher>();
builder.Services.AddSingleton<Doc2CodeHandlers>();

var app = builder.Build();

// Initialize the chat client provider from persisted settings at startup
var chatClientProvider = app.Services.GetRequiredService<ChatClientProvider>();
try
{
    chatClientProvider.RefreshAsync().GetAwaiter().GetResult();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to initialize ChatClientProvider at startup — will use defaults");
}

// aspire map default endpoints
app.MapDefaultEndpoints();

app.UseApiKeyAuth();

app.MapDoc2CodeRoutes();
app.Run();

