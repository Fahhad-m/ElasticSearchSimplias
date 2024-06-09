using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Service;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Specialized;
using SearchAPI.Models;

var builder = WebApplication.CreateBuilder(args);



builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddControllers();
var elasettings = new ElasticSettings()
{
    SqlDBConnection = builder.Configuration.GetConnectionString("DefaultConnection")?? string.Empty,
    ApiKey = builder.Configuration["Elasticsearch:ApiKey"] ?? string.Empty,
    ElaUri = new Uri(builder.Configuration["Elasticsearch:Uri"]) ,
Username = builder.Configuration["Elasticsearch:Username"] ?? string.Empty,
    ApiValue = builder.Configuration["Elasticsearch:Value"] ?? string.Empty,
    ElaIndex = builder.Configuration["Elasticsearch:Index"] ?? string.Empty,
    ReactUrl = builder.Configuration["React:PageUrl"] ?? string.Empty,
};
var settings = new ConnectionSettings(elasettings.ElaUri).DefaultIndex(elasettings.ElaIndex)
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
builder.Services.AddScoped<IProductService, ProductService>(provider =>
    new ProductService(client, provider.GetRequiredService<ILogger<ProductService>>(), elasettings));
builder.Services.AddSingleton<IProductService>(provider =>
    new ProductService(client, provider.GetRequiredService<ILogger<ProductService>>(), elasettings));

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
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapControllers();
app.UseStaticFiles();
app.UseSpaStaticFiles();

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    if (app.Environment.IsDevelopment())
    {
        spa.UseProxyToSpaDevelopmentServer(elasettings.ReactUrl);
    }
});
app.Run();