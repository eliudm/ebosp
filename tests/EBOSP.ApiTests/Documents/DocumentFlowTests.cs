using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Documents;
using EBOSP.Contracts.Procurement;

namespace EBOSP.ApiTests.Documents;

/// <summary>Spec §18's document management - upload authorized via the parent entity's own write permission, download via its own read gate.</summary>
public class DocumentFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly byte[] ValidPdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0x25, 0x45, 0x4F, 0x46];

    [Fact]
    public async Task Upload_ToOwnedPurchaseOrder_SucceedsAndDownloadReturnsOriginalBytes()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var orderId = await CreatePurchaseOrderAsync(adminClient);

        var uploadResponse = await UploadAsync(adminClient, "PurchaseOrder", orderId.ToString(), "invoice.pdf", "application/pdf", ValidPdfBytes);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var document = (await uploadResponse.Content.ReadFromJsonAsync<DocumentResponse>())!;
        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal(ValidPdfBytes.Length, document.SizeBytes);

        var downloadResponse = await adminClient.GetAsync($"/api/v1/documents/{document.Id}/content");
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(ValidPdfBytes, downloadedBytes);
    }

    [Fact]
    public async Task Upload_WithoutRequiredPermission_Returns403()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var orderId = await CreatePurchaseOrderAsync(adminClient);
        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        var response = await UploadAsync(salesClient, "PurchaseOrder", orderId.ToString(), "invoice.pdf", "application/pdf", ValidPdfBytes);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Upload_MismatchedSignature_Returns400()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var orderId = await CreatePurchaseOrderAsync(adminClient);

        // MZ - an executable header, claiming to be a .pdf.
        byte[] executableBytes = [0x4D, 0x5A, 0x90, 0x00];
        var response = await UploadAsync(adminClient, "PurchaseOrder", orderId.ToString(), "invoice.pdf", "application/pdf", executableBytes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_DisallowedExtension_Returns400()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var orderId = await CreatePurchaseOrderAsync(adminClient);

        var response = await UploadAsync(adminClient, "PurchaseOrder", orderId.ToString(), "malware.exe", "application/octet-stream", [0x4D, 0x5A]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_UnsupportedEntityType_Returns400()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        var response = await UploadAsync(adminClient, "Customer", Guid.NewGuid().ToString(), "invoice.pdf", "application/pdf", ValidPdfBytes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDocument_AnotherTenants_Returns404()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var orderId = await CreatePurchaseOrderAsync(tenantAClient);
        var uploadResponse = await UploadAsync(tenantAClient, "PurchaseOrder", orderId.ToString(), "invoice.pdf", "application/pdf", ValidPdfBytes);
        var document = (await uploadResponse.Content.ReadFromJsonAsync<DocumentResponse>())!;

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);

        var response = await tenantBClient.GetAsync($"/api/v1/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string entityType, string entityId, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(entityType), "entityType" },
            { new StringContent(entityId), "entityId" },
        };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        return await client.PostAsync("/api/v1/documents", content);
    }

    private async Task<Guid> CreatePurchaseOrderAsync(HttpClient client)
    {
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(client);
        // A separate requester, distinct from the approving `client` - approving one's own request
        // is denied (PurchaseRequestService's self-approval guard).
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(client, factory, "procurement");
        var requestResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await requestResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        (await client.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();

        var supplierResponse = await client.PostAsJsonAsync("/api/v1/suppliers", new CreateSupplierRequest { Name = "Supplier " + Guid.NewGuid() });
        var supplier = (await supplierResponse.Content.ReadFromJsonAsync<SupplierResponse>())!;

        var orderResponse = await client.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplier.Id,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var order = (await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
        return order.Id;
    }
}
