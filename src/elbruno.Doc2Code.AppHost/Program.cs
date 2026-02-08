var builder = DistributedApplication.CreateBuilder(args);

//// LLM inference container with a persistent volume for downloaded models
//var ollamaContainer = builder.AddOllama("ollama")
//    .WithImageTag("latest")
//    .WithDataVolume()
//    .WithGPUSupport(OllamaGpuVendor.Nvidia); 
//var llmModel = ollamaContainer.AddModel("ministral-3");

// Settings microservice — persists Doc2CodeConfig to JSON file
var settings = builder.AddProject<Projects.elbruno_Doc2Code_SettingsService>("settingsservice");

// API backend — depends on the LLM model and settings service
var backend = builder.AddProject<Projects.elbruno_Doc2Code_ApiService>("apiservice")
    //.WithReference(llmModel).WaitFor(llmModel)
    .WithReference(settings).WaitFor(settings);

// Blazor UI — depends on the backend and settings service
builder.AddProject<Projects.elbruno_Doc2Code_Web>("webfrontend")
 .WithExternalHttpEndpoints()
 .WithReference(backend).WaitFor(backend)
 .WithReference(settings).WaitFor(settings);

builder.Build().Run();
