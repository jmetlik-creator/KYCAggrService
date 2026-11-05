using KycAggregation.Data;
using Logging.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(configuredConnectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
}

builder.Services.AddDbContext<KycDbContext>(options => options.UseSqlServer(configuredConnectionString));
builder.Services.Configure<KycDataCacheOptions>(builder.Configuration.GetSection("KycDataCache"));
builder.Services.AddScoped<IKycDataRepository, KycDataRepository>();
builder.Services.AddScoped<KycAggregationService>();

builder.Services.AddHttpClient<ICustomerDataClient, CustomerDataClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["CustomerDataApi:BaseUrl"]
        ?? "https://customerdataapi-codetest-bughceejfwbwbkb9.swedencentral-01.azurewebsites.net";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ILogSink, HttpLogSinkClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["LoggingService:BaseUrl"] ?? "http://localhost:5201";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KycDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
