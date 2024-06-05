using Nest;
using SearchAPI.Interfaces;
using SearchAPI.Service;
using Microsoft.AspNetCore.SpaServices.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);


// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var elasticsearchUri = new Uri(builder.Configuration["Elasticsearch:Uri"]);
var settings = new ConnectionSettings(elasticsearchUri).DefaultIndex("products");
var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);
//builder.Services.AddScoped<IProductService, ProductService>();


builder.Services.AddScoped<IProductService, ProductService>(provider =>
    new ProductService(client, provider.GetRequiredService<ILogger<ProductService>>(), connectionString));

builder.Services.AddSingleton<IProductService>(provider =>
    new ProductService(client, provider.GetRequiredService<ILogger<ProductService>>(), connectionString));

//builder.Services.AddScoped<IProductService, ProductService>(provider => new ProductService(connectionString));
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();


// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "ClientApp/build";
});
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom exception handling middleware
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
// Register the SPA static files middleware

app.UseHttpsRedirection();

//app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles();
app.UseSpaStaticFiles();

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
    if (app.Environment.IsDevelopment())
    {
        spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
    }
});
app.Run();