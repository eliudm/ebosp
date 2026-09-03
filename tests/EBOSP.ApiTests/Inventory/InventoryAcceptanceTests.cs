using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Inventory;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EBOSP.ApiTests.Inventory;

/// <summary>Dev guide §13.4 "Inventory acceptance tests", almost verbatim.</summary>
public class InventoryAcceptanceTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Receive100Units_BalanceIncreasesByExactly100()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 100 }],
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(100, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task Issue20Units_BalanceDecreasesByExactly20()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 100);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/issues", new IssueStockRequest { WarehouseId = warehouseId, ProductId = productId, Quantity = 20 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(80, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task Transfer10Units_AtomicallyMovesBetweenWarehouses()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseA, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        var branchId = await InventoryTestHelpers.CreateBranchAsync(client);
        var warehouseB = await InventoryTestHelpers.CreateWarehouseAsync(client, branchId);
        await ReceiveAsync(client, warehouseA, productId, 50);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new TransferStockRequest
        {
            FromWarehouseId = warehouseA,
            ToWarehouseId = warehouseB,
            ProductId = productId,
            Quantity = 10,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(40, await GetOnHandAsync(client, warehouseA, productId));
        Assert.Equal(10, await GetOnHandAsync(client, warehouseB, productId));
    }

    [Fact]
    public async Task UnauthorizedAdjustment_NoStockMutation()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 50);

        var noRoleEmail = $"norole-{Guid.NewGuid():N}@example.com";
        var createUserResponse = await client.PostAsJsonAsync("/api/v1/users", new EBOSP.Contracts.Identity.CreateUserRequest { Email = noRoleEmail, Password = "SomePassword123!" });
        createUserResponse.EnsureSuccessStatusCode();
        var noRoleClient = factory.CreateClient();
        var noRoleTokens = await AuthTestHelpers.LoginAsync(noRoleClient, noRoleEmail, "SomePassword123!");
        AuthTestHelpers.AuthorizeAs(noRoleClient, noRoleTokens);

        var response = await noRoleClient.PostAsJsonAsync("/api/v1/inventory/adjustments", new AdjustStockRequest
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityDelta = 5,
            Reason = "Should not apply",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(50, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task DuplicateIdempotencyKey_NoDoubleMovement()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        var key = Guid.NewGuid().ToString();

        var request = new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            IdempotencyKey = key,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 100 }],
        };

        var first = await client.PostAsJsonAsync("/api/v1/inventory/receipts", request);
        var second = await client.PostAsJsonAsync("/api/v1/inventory/receipts", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(100, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task AdjustmentAboveThreshold_RequiresInventoryAdjustLarge()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 1000);

        // Default threshold is 100 - a warehouse-manager-only role (inventory.adjust, no
        // inventory.adjust.large) should be blocked from a delta that exceeds it.
        var managerEmail = $"manager-{Guid.NewGuid():N}@example.com";
        var createUserResponse = await client.PostAsJsonAsync("/api/v1/users", new EBOSP.Contracts.Identity.CreateUserRequest { Email = managerEmail, Password = "SomePassword123!" });
        var created = (await createUserResponse.Content.ReadFromJsonAsync<EBOSP.Contracts.Identity.UserResponse>())!;
        await client.PostAsJsonAsync($"/api/v1/users/{created.Id}/roles", new EBOSP.Contracts.Identity.AssignRoleRequest { RoleCode = "warehouse-manager" });
        await client.PostAsync($"/api/v1/users/{created.Id}/activate", content: null);

        var managerClient = factory.CreateClient();
        var managerTokens = await AuthTestHelpers.LoginAsync(managerClient, managerEmail, "SomePassword123!");
        AuthTestHelpers.AuthorizeAs(managerClient, managerTokens);

        var largeAdjustment = new AdjustStockRequest { WarehouseId = warehouseId, ProductId = productId, QuantityDelta = -150, Reason = "Large correction" };
        var deniedResponse = await managerClient.PostAsJsonAsync("/api/v1/inventory/adjustments", largeAdjustment);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        // The tenant-admin holds inventory.adjust.large and can perform it.
        var allowedResponse = await client.PostAsJsonAsync("/api/v1/inventory/adjustments", largeAdjustment);
        Assert.Equal(HttpStatusCode.Created, allowedResponse.StatusCode);
        Assert.Equal(850, await GetOnHandAsync(client, warehouseId, productId));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await context.OutboxMessages.AnyAsync(m => m.TenantId == admin.TenantId && m.EventType == "StockAdjustmentFlagged"));
    }

    [Fact]
    public async Task LowStockCrossing_EmitsExactlyOneLowStockDetectedEvent()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client, reorderLevel: 10);
        await ReceiveAsync(client, warehouseId, productId, 20);

        // Crosses the threshold (20 -> 5).
        await client.PostAsJsonAsync("/api/v1/inventory/issues", new IssueStockRequest { WarehouseId = warehouseId, ProductId = productId, Quantity = 15 });
        // Stays below the threshold (5 -> 3) - must not raise a second alert.
        await client.PostAsJsonAsync("/api/v1/inventory/issues", new IssueStockRequest { WarehouseId = warehouseId, ProductId = productId, Quantity = 2 });

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await context.OutboxMessages.CountAsync(m => m.TenantId == admin.TenantId && m.EventType == "LowStockDetected" && m.AggregateId == productId.ToString());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConcurrentIssues_ProduceCorrectFinalBalance()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 1000);

        var tasks = Enumerable.Range(0, 10).Select(_ =>
        {
            var issueClient = factory.CreateClient();
            AuthTestHelpers.AuthorizeAs(issueClient, admin.Tokens);
            return issueClient.PostAsJsonAsync("/api/v1/inventory/issues", new IssueStockRequest { WarehouseId = warehouseId, ProductId = productId, Quantity = 10 });
        });

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(900, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task Issue_MoreThanAvailable_Returns409AndDoesNotMutate()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 10);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/issues", new IssueStockRequest { WarehouseId = warehouseId, ProductId = productId, Quantity = 11 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(10, await GetOnHandAsync(client, warehouseId, productId));
    }

    [Fact]
    public async Task Transfer_WithAnotherTenantsWarehouse_Returns404()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (warehouseId, productId) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(client);
        await ReceiveAsync(client, warehouseId, productId, 10);

        var otherClient = factory.CreateClient();
        var otherAdmin = await AuthTestHelpers.CreateTenantAdminAsync(otherClient);
        AuthTestHelpers.AuthorizeAs(otherClient, otherAdmin.Tokens);
        var (otherWarehouseId, _) = await InventoryTestHelpers.CreateWarehouseAndProductAsync(otherClient);

        var response = await client.PostAsJsonAsync("/api/v1/inventory/transfers", new TransferStockRequest
        {
            FromWarehouseId = warehouseId,
            ToWarehouseId = otherWarehouseId,
            ProductId = productId,
            Quantity = 5,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task ReceiveAsync(HttpClient client, Guid warehouseId, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = quantity }],
        });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<int> GetOnHandAsync(HttpClient client, Guid warehouseId, Guid productId)
    {
        var response = await client.GetAsync($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        response.EnsureSuccessStatusCode();
        var page = (await response.Content.ReadFromJsonAsync<EBOSP.Contracts.Common.PagedResult<StockBalanceResponse>>())!;
        return page.Items.Single().QuantityOnHand;
    }
}
