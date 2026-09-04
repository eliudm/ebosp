using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Documents;
using EBOSP.Contracts.Procurement;

namespace EBOSP.E2ETests.Journeys;

/// <summary>Dev guide §25.3 "Document upload -> authorized download" - built on a real owned entity (a purchase order), authorized via that entity's own write permission.</summary>
public class DocumentUploadToDownloadTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly byte[] ValidPdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0x25, 0x45, 0x4F, 0x46];

    [Fact]
    public async Task FullJourney_CreatePurchaseOrderUploadDocumentThenDownloadIt_BytesMatch()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");

        var requestResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "E2E journey document upload",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await requestResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();
        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(adminClient);
        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-E2E-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var order = (await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;

        using var content = new MultipartFormDataContent
        {
            { new StringContent("PurchaseOrder"), "entityType" },
            { new StringContent(order.Id.ToString()), "entityId" },
        };
        var fileContent = new ByteArrayContent(ValidPdfBytes);
        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/pdf");
        content.Add(fileContent, "file", "supplier-invoice.pdf");

        var uploadResponse = await adminClient.PostAsync("/api/v1/documents", content);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var document = (await uploadResponse.Content.ReadFromJsonAsync<DocumentResponse>())!;

        var downloadResponse = await adminClient.GetAsync($"/api/v1/documents/{document.Id}/content");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

        Assert.Equal(ValidPdfBytes, downloadedBytes);
    }
}
