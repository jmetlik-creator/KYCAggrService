using KycAggregation.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "kyc-data.db")}";

builder.Services.AddDbContext<KycDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IKycDataRepository, KycDataRepository>();
builder.Services.AddScoped<KycAggregationService>();

builder.Services.AddHttpClient<ICustomerDataClient, CustomerDataClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["CustomerDataApi:BaseUrl"]
        ?? "https://customerdataapi-codetest-bughceejfwbwbkb9.swedencentral-01.azurewebsites.net";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
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

app.MapGet("/kyc-data/{ssn}", async ([FromRoute] string ssn, KycAggregationService service, ILogger<Program> logger) =>
{
    try
    {
        var response = await service.GetAsync(ssn);
        return Results.Ok(response);
    }
    catch (InvalidSsnException ex)
    {
        logger.LogWarning(ex, "Invalid SSN supplied.");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (CustomerNotFoundException ex)
    {
        logger.LogWarning(ex, "Customer data not found for SSN {Ssn}.", ssn);
        return Results.NotFound(new { error = "Customer data not found for the provided SSN." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error while processing KYC request for SSN {Ssn}.", ssn);
        return Results.Problem(
            detail: "An unexpected error occurred while processing the request.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal server error");
    }
})
.WithName("GetAggregatedKycData");

app.Run();
