using System.Net.Http.Json;
using EBOSP.Contracts.Identity;
using EBOSP.Contracts.MasterData;
using EBOSP.Contracts.Procurement;

namespace EBOSP.ApiTests.Procurement;

internal sealed record BranchWarehouseAndProduct(Guid BranchId, Guid WarehouseId, Guid ProductId);

internal static class ProcurementTestHelpers
{
    public const string UserPassword = "SomePassword123!";

    public static async Task<BranchWarehouseAndProduct> CreateBranchWarehouseAndProductAsync(HttpClient client)
    {
        var branchResponse = await client.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Branch " + Guid.NewGuid() });
        branchResponse.EnsureSuccessStatusCode();
        var branch = (await branchResponse.Content.ReadFromJsonAsync<BranchResponse>())!;

        var warehouseResponse = await client.PostAsJsonAsync("/api/v1/warehouses", new CreateWarehouseRequest { BranchId = branch.Id, Name = "Warehouse " + Guid.NewGuid() });
        warehouseResponse.EnsureSuccessStatusCode();
        var warehouse = (await warehouseResponse.Content.ReadFromJsonAsync<WarehouseResponse>())!;

        var productResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Product " + Guid.NewGuid(),
            UnitPrice = 10m,
            TaxRatePercent = 16m,
            ReorderLevel = 0,
        });
        productResponse.EnsureSuccessStatusCode();
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        return new BranchWarehouseAndProduct(branch.Id, warehouse.Id, product.Id);
    }

    public static async Task<Guid> CreateSupplierAsync(HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/suppliers", new CreateSupplierRequest { Name = name ?? "Supplier " + Guid.NewGuid() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupplierResponse>())!.Id;
    }

    /// <summary>Creates a user, assigns the given role, activates it and logs in - returns an authorized client and the user's id.</summary>
    public static async Task<(HttpClient Client, Guid UserId)> CreateUserWithRoleAsync(HttpClient adminClient, CustomWebApplicationFactory factory, string roleCode)
    {
        var email = $"{roleCode}-{Guid.NewGuid():N}@example.com";
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/users", new CreateUserRequest { Email = email, Password = UserPassword });
        createResponse.EnsureSuccessStatusCode();
        var created = (await createResponse.Content.ReadFromJsonAsync<UserResponse>())!;

        (await adminClient.PostAsJsonAsync($"/api/v1/users/{created.Id}/roles", new AssignRoleRequest { RoleCode = roleCode })).EnsureSuccessStatusCode();
        (await adminClient.PostAsync($"/api/v1/users/{created.Id}/activate", content: null)).EnsureSuccessStatusCode();

        var userClient = factory.CreateClient();
        var tokens = await AuthTestHelpers.LoginAsync(userClient, email, UserPassword);
        AuthTestHelpers.AuthorizeAs(userClient, tokens);

        return (userClient, created.Id);
    }
}
