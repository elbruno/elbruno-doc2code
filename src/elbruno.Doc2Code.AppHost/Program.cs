var builder = DistributedApplication.CreateBuilder(args);

// Shared API key — Aspire prompts on first run and stores in user secrets
var apiKey = builder.AddParameter("apikey", secret: true);

//// LLM inference container with a persistent volume for downloaded models
//var ollamaContainer = builder.AddOllama("ollama")
//    .WithImageTag("latest")
//    .WithDataVolume()
//    .WithGPUSupport(OllamaGpuVendor.Nvidia); 
//var llmModel = ollamaContainer.AddModel("ministral-3");

// Settings microservice — persists Doc2CodeConfig to JSON file
var settings = builder.AddProject<Projects.elbruno_Doc2Code_SettingsService>("settingsservice")
    .WithEnvironment("ApiKey", apiKey);

// API backend — depends on the LLM model and settings service
var backend = builder.AddProject<Projects.elbruno_Doc2Code_ApiService>("apiservice")
    //.WithReference(llmModel).WaitFor(llmModel)
    .WithReference(settings).WaitFor(settings)
    .WithEnvironment("ApiKey", apiKey);

// Blazor UI — depends on the backend and settings service
builder.AddProject<Projects.elbruno_Doc2Code_Web>("webfrontend")
 .WithExternalHttpEndpoints()
 .WithReference(backend).WaitFor(backend)
 .WithReference(settings).WaitFor(settings)
 .WithEnvironment("ApiKey", apiKey);

builder.Build().Run();
