using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Procurement;

namespace EBOSP.E2ETests.Journeys;

/// <summary>Dev guide §25.3 "Purchase request -> approval -> PO -> receipt" - one continuous chain, each step feeding the next.</summary>
public class PurchaseRequestToReceiptTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullJourney_RequestApprovePoReceive_StockBalanceReflectsTheReceipt()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        // A separate requester, distinct from the approving admin - approving one's own request is denied.
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");

        var requestResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "E2E journey restock",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 20, EstimatedUnitPrice = 5m }],
        });
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var request = (await requestResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;

        var approveResponse = await adminClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(adminClient);
        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-E2E-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 20, UnitPrice = 5m }],
        });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = (await orderResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;

        var receiptResponse = await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            PurchaseOrderId = order.Id,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 20 }],
        });
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);

        var balancesResponse = await adminClient.GetAsync($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        Assert.Equal(HttpStatusCode.OK, balancesResponse.StatusCode);
        var balances = (await balancesResponse.Content.ReadFromJsonAsync<EBOSP.Contracts.Common.PagedResult<StockBalanceResponse>>())!;
        Assert.Equal(20, Assert.Single(balances.Items).QuantityOnHand);
    }
}
