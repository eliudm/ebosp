using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.MasterData;

namespace EBOSP.ApiTests.MasterData;

public class ProductsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateProduct_WithOwnTenantCategory_Succeeds()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var categoryResponse = await client.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest { Name = "Beverages" });
        var category = (await categoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var productResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            CategoryId = category.Id,
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Sparkling Water",
            UnitPrice = 1.50m,
            TaxRatePercent = 16m,
            ReorderLevel = 20,
        });

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = (await productResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal(category.Id, product.CategoryId);
    }

    [Fact]
    public async Task CreateProduct_WithAnotherTenantsCategory_Returns404()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var otherClient = factory.CreateClient();
        var otherAdmin = await AuthTestHelpers.CreateTenantAdminAsync(otherClient);
        AuthTestHelpers.AuthorizeAs(otherClient, otherAdmin.Tokens);
        var otherCategoryResponse = await otherClient.PostAsJsonAsync("/api/v1/product-categories", new CreateProductCategoryRequest { Name = "Other Tenant Category" });
        var otherCategory = (await otherCategoryResponse.Content.ReadFromJsonAsync<ProductCategoryResponse>())!;

        var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            CategoryId = otherCategory.Id,
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Cross Tenant Product",
            UnitPrice = 1m,
            TaxRatePercent = 16m,
            ReorderLevel = 5,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSkuWithinSameTenant_Returns409()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var sku = $"SKU-{Guid.NewGuid():N}";

        var request = new CreateProductRequest { Sku = sku, Name = "Product One", UnitPrice = 1m, TaxRatePercent = 16m, ReorderLevel = 5 };
        var firstResponse = await client.PostAsJsonAsync("/api/v1/products", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/products", request);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task SameSku_AcrossDifferentTenants_BothSucceed()
    {
        var sku = $"SKU-{Guid.NewGuid():N}";

        var clientA = factory.CreateClient();
        var adminA = await AuthTestHelpers.CreateTenantAdminAsync(clientA);
        AuthTestHelpers.AuthorizeAs(clientA, adminA.Tokens);

        var clientB = factory.CreateClient();
        var adminB = await AuthTestHelpers.CreateTenantAdminAsync(clientB);
        AuthTestHelpers.AuthorizeAs(clientB, adminB.Tokens);

        var request = new CreateProductRequest { Sku = sku, Name = "Shared SKU Product", UnitPrice = 1m, TaxRatePercent = 16m, ReorderLevel = 5 };
        var responseA = await clientA.PostAsJsonAsync("/api/v1/products", request);
        var responseB = await clientB.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, responseB.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_ChangesFields()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var createResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Original Name",
            UnitPrice = 1m,
            TaxRatePercent = 16m,
            ReorderLevel = 5,
        });
        var created = (await createResponse.Content.ReadFromJsonAsync<ProductResponse>())!;

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{created.Id}", new UpdateProductRequest
        {
            Name = "Updated Name",
            UnitPrice = 2.50m,
            TaxRatePercent = 8m,
            ReorderLevel = 15,
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ProductResponse>())!;
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(2.50m, updated.UnitPrice);
        Assert.Equal(8m, updated.TaxRatePercent);
        Assert.Equal(15, updated.ReorderLevel);
        Assert.Equal(created.Sku, updated.Sku);
    }
}
