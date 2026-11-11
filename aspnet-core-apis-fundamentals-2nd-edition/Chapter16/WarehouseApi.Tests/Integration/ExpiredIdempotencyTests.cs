using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WarehouseApi.Contracts;
using WarehouseApi.Models;
using WarehouseApi.Persistence;
using WarehouseApi.Services;
using WarehouseApi.Tests.Infrastructure;

namespace WarehouseApi.Tests.Integration;

public class ExpiredIdempotencyTests(WarehouseApiFactory factory) : IClassFixture<WarehouseApiFactory>
{
    [Fact]
    public async Task TwoOrdersReusingAnExpiredKey_CreateOneOrderAndReplayIt()
    {
        var key = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest("EXPIRED-ORDER", null, [new OrderLineRequest("SKU-001", 1)]);
        await SeedExpiredKeyAsync(key);

        var gate = new TwoWriters();
        await using var first = Open(gate);
        await using var second = Open(gate);
        var results = await Task.WhenAll(CreateOrderAsync(first, request, key), CreateOrderAsync(second, request, key));

        var winner = Assert.IsType<OrderResponse>(results[0]);
        var replay = Assert.IsType<OrderResponse>(results[1]);
        Assert.Equal(winner.Id, replay.Id);
        await using var check = factory.CreateDbContext();
        Assert.Equal(1, await check.Orders.CountAsync(o => o.CustomerReference == request.CustomerReference));
    }

    [Fact]
    public async Task OrderAndAdjustmentReusingAnExpiredKey_OnlyTheWinnerChangesData()
    {
        var key = Guid.NewGuid().ToString();
        var request = new CreateOrderRequest("EXPIRED-MIXED", null, [new OrderLineRequest("SKU-001", 1)]);
        await SeedExpiredKeyAsync(key);
        await using var before = factory.CreateDbContext();
        var onHand = await OnHandAsync(before);
        var history = await before.StockAdjustments.CountAsync();

        var gate = new TwoWriters();
        await using var first = Open(gate);
        await using var second = Open(gate);
        var order = CreateOrderAsync(first, request, key);
        var stock = Inventory(second).AdjustStockAsync("WH-001", "SKU-001", 3, "RECEIPT", key);
        await Task.WhenAll(order, stock);

        var orderWon = await order is not null;
        var stockResult = await stock;
        Assert.Equal(orderWon ? 422 : 200, stockResult.StatusCode);
        await using var check = factory.CreateDbContext();
        Assert.Equal(orderWon ? 1 : 0, await check.Orders.CountAsync(o => o.CustomerReference == request.CustomerReference));
        Assert.Equal(onHand + (orderWon ? 0 : 3), await OnHandAsync(check));
        Assert.Equal(history + (orderWon ? 0 : 1), await check.StockAdjustments.CountAsync());
        var retry = await Inventory(check).AdjustStockAsync("WH-001", "SKU-001", 3, "RECEIPT", key);
        Assert.Equal(stockResult.StatusCode, retry.StatusCode);
        Assert.Equal(onHand + (orderWon ? 0 : 3), await OnHandAsync(check));
    }

    private async Task SeedExpiredKeyAsync(string key)
    {
        await using var db = factory.CreateDbContext();
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            RequestHash = "expired",
            StatusCode = 201,
            ResponseBody = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        await db.SaveChangesAsync();
    }

    private WarehouseDbContext Open(TwoWriters gate) =>
        new(new DbContextOptionsBuilder<WarehouseDbContext>().UseSqlServer(factory.ConnectionString)
            .AddInterceptors(gate).Options);

    private static Task<int> OnHandAsync(WarehouseDbContext db) => db.StockItems.AsNoTracking()
        .Where(s => s.WarehouseCode == "WH-001" && s.ProductSku == "SKU-001")
        .Select(s => s.OnHand).SingleAsync();

    private static EfInventoryService Inventory(WarehouseDbContext db) => new(db, new CorrelationContext());

    private static Task<OrderResponse?> CreateOrderAsync(WarehouseDbContext db, CreateOrderRequest request, string key) =>
        new EfOrderService(db, Inventory(db)).CreateAsync(request, key, "test-caller", CancellationToken.None);

    // Both contexts must have read and staged changes to the same old record before either can write.
    private sealed class TwoWriters : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _writers;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _writers) == 2)
                _ready.TrySetResult();

            await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            return result;
        }
    }
}
