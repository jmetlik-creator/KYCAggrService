using System.Text.Json;
using KycAggregation.Data;
using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Tests;

public sealed class KycDataRepositoryTests
{
    [Fact]
    public async Task SavePersonalDetailsAsync_WhenCalled_StoresPayloadAndTransactionId()
    {
        var options = new DbContextOptionsBuilder<KycDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new KycDbContext(options);
        var repository = new KycDataRepository(context);
        var transactionId = Guid.NewGuid();

        await repository.SavePersonalDetailsAsync(new CustomerPersonalDetailsRecord
        {
            Ssn = "19800115-1234",
            TransactionId = transactionId,
            PayloadJson = "{\"firstName\":\"Erik\"}",
            FetchedUtc = DateTime.UtcNow
        });

        var persisted = await repository.GetPersonalDetailsAsync("19800115-1234");

        Assert.NotNull(persisted);
        Assert.Equal(transactionId, persisted!.TransactionId);
        Assert.Equal("Erik", JsonDocument.Parse(persisted.PayloadJson).RootElement.GetProperty("firstName").GetString());
    }

    [Fact]
    public async Task SaveAggregationAsync_WhenMultipleResultsExist_ReturnsLatestByCreatedUtc()
    {
        var options = new DbContextOptionsBuilder<KycDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new KycDbContext(options);
        var repository = new KycDataRepository(context);

        var first = new KycAggregationResultRecord
        {
            Ssn = "19800115-1234",
            TransactionId = Guid.NewGuid(),
            AggregatedPayloadJson = "{\"firstName\":\"Old\"}",
            FetchState = FetchSourceState.FetchedFromDb.ToString(),
            CreatedUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var second = new KycAggregationResultRecord
        {
            Ssn = "19800115-1234",
            TransactionId = Guid.NewGuid(),
            AggregatedPayloadJson = "{\"firstName\":\"Fresh\"}",
            FetchState = FetchSourceState.DirectFromService.ToString(),
            CreatedUtc = DateTime.UtcNow
        };

        await repository.SaveAggregationAsync(first);
        await repository.SaveAggregationAsync(second);

        var latest = await repository.GetLatestAggregationAsync("19800115-1234");

        Assert.NotNull(latest);
        Assert.Equal("Fresh", JsonDocument.Parse(latest!.AggregatedPayloadJson).RootElement.GetProperty("firstName").GetString());
    }
}
