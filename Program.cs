using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Service;
using Microsoft.AspNetCore.SpaServices.Extensions;
using System.Collections.Specialized;
using SearchAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var elasticUri = builder.Configuration["Elasticsearch:Uri"];
if (string.IsNullOrWhiteSpace(elasticUri))
    throw new InvalidOperationException("Elasticsearch:Uri is not configured in appsettings.json.");

var elasettings = new ElasticSettings()
{
    SqlDBConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
    ApiKey = builder.Configuration["Elasticsearch:ApiKey"] ?? string.Empty,
    ElaUri = new Uri(elasticUri),
    Username = builder.Configuration["Elasticsearch:Username"] ?? string.Empty,
    ApiValue = builder.Configuration["Elasticsearch:Value"] ?? string.Empty,
    ElaIndex = builder.Configuration["Elasticsearch:Index"] ?? string.Empty,
    ReactUrl = builder.Configuration["React:PageUrl"] ?? string.Empty,
};

var settings = new ConnectionSettings(elasettings.ElaUri)
    .DefaultIndex(elasettings.ElaIndex)
    .ThrowExceptions(alwaysThrow: true)
    .PrettyJson()
    .RequestTimeout(TimeSpan.FromSeconds(300))
    .ApiKeyAuthentication(elasettings.ApiKey, elasettings.ApiValue)
    .GlobalHeaders(new NameValueCollection
    {
        { elasettings.ApiKey, elasettings.ApiValue }
    });

var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);
builder.Services.AddSingleton(elasettings);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();
builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "ClientApp/build";
});

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
        spa.UseProxyToSpaDevelopmentServer(elasettings.ReactUrl);
    }
});

app.Run();