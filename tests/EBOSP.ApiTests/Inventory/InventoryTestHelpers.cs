using System.Net.Http.Json;
using EBOSP.Contracts.MasterData;

namespace EBOSP.ApiTests.Inventory;

internal sealed record WarehouseAndProduct(Guid WarehouseId, Guid ProductId);

internal static class InventoryTestHelpers
{
    public static async Task<WarehouseAndProduct> CreateWarehouseAndProductAsync(HttpClient client, int reorderLevel = 0)
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
            ReorderLevel = reorderLevel,
        });
        productResponse.EnsureSuccessStatusCode();
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        return new WarehouseAndProduct(warehouse.Id, product.Id);
    }

    public static async Task<Guid> CreateWarehouseAsync(HttpClient client, Guid branchId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/warehouses", new CreateWarehouseRequest { BranchId = branchId, Name = "Warehouse " + Guid.NewGuid() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WarehouseResponse>())!.Id;
    }

    public static async Task<Guid> CreateBranchAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/branches", new CreateBranchRequest { Name = "Branch " + Guid.NewGuid() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BranchResponse>())!.Id;
    }
}
