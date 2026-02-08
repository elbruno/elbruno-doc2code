// elbruno.Doc2Code SettingsService entry point.
using elbruno.Doc2Code.Core.Abstractions;
using elbruno.Doc2Code.SettingsService;
using elbruno.Doc2Code.SettingsService.Services;

using elbruno.Doc2Code.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// add aspire service defaults
builder.AddServiceDefaults();
builder.AddApiKeyAuth();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<ISettingsStore>(sp =>
{
    var dataDir = builder.Configuration["SettingsService:DataDirectory"] ?? "data";
    return new SettingsStore(dataDir);
});
builder.Services.AddSignalR();

var app = builder.Build();


// aspire map default endpoints
app.MapDefaultEndpoints();

app.UseApiKeyAuth();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapSettingsRoutes();

app.Run();
