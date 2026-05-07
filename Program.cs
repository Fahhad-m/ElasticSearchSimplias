using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Repositories;
using SearchAPI.Service;
using Microsoft.AspNetCore.SpaServices.Extensions;
using System.Collections.Specialized;
using SearchAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var elasticUri = builder.Configuration["Elasticsearch:Uri"];
if (string.IsNullOrWhiteSpace(elasticUri))
    throw new InvalidOperationException("Elasticsearch:Uri must be configured in appsettings.json.");

var appSettings = new ElasticSettings
{
    SqlDBConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
    ApiKey = builder.Configuration["Elasticsearch:ApiKey"] ?? string.Empty,
    ElaUri = new Uri(elasticUri),
    Username = builder.Configuration["Elasticsearch:Username"] ?? string.Empty,
    ApiValue = builder.Configuration["Elasticsearch:Value"] ?? string.Empty,
    ElaIndex = builder.Configuration["Elasticsearch:Index"] ?? string.Empty,
    ReactUrl = builder.Configuration["React:PageUrl"] ?? string.Empty,
};

// ── Elasticsearch NEST Client ─────────────────────────────────────────────────
// ThrowExceptions = false: we check IsValid on every response ourselves.
// This gives us consistent error handling instead of surprise exceptions from NEST.
var connectionSettings = new ConnectionSettings(appSettings.ElaUri)
    .DefaultIndex(appSettings.ElaIndex)
    .ThrowExceptions(alwaysThrow: false)
    .PrettyJson()
    .RequestTimeout(TimeSpan.FromSeconds(30))
    .ApiKeyAuthentication(appSettings.ApiKey, appSettings.ApiValue)
    .GlobalHeaders(new NameValueCollection
    {
        { appSettings.ApiKey, appSettings.ApiValue }
    });

var elasticClient = new ElasticClient(connectionSettings);

// ── Dependency Injection ──────────────────────────────────────────────────────
// Singleton: shared across all requests (thread-safe, stateless)
builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton<IElasticClient>(elasticClient);

// Scoped: one instance per HTTP request (safe for SqlConnection per-call pattern)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISyncOutboxRepository, SyncOutboxRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

// Background service: outbox processor + initial sync
builder.Services.AddHostedService<ElasticsearchSyncBackgroundService>();

builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "ClientApp/build";
});

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles();
app.UseSpaStaticFiles();
app.MapControllers();

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    if (app.Environment.IsDevelopment())
    {
        spa.UseProxyToSpaDevelopmentServer(appSettings.ReactUrl);
    }
});

app.Run();