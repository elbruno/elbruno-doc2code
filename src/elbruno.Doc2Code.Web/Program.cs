using elbruno.Doc2Code.Web;
using elbruno.Doc2Code.Web.Components;
using elbruno.Doc2Code.Web.Services;
using elbruno.Doc2Code.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// add aspire service defaults
builder.AddServiceDefaults();
builder.AddApiKeyAuth();

builder.Services.AddScoped<SettingsApiClient>();
builder.Services.AddScoped<GenerationApiClient>();
builder.Services.AddScoped<AgentLogSignalRService>();

builder.Services.AddTransient<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient<SettingsApiClient>(
    static client => client.BaseAddress = new ("https+http://settingsservice"))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient<GenerationApiClient>(
    static client => client.BaseAddress = new Uri("https+http://apiservice"))
    .AddHttpMessageHandler<ApiKeyDelegatingHandler>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// aspire map default endpoints
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
