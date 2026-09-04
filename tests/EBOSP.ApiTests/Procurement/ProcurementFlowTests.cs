using System.Net;
using System.Net.Http.Json;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Procurement;

namespace EBOSP.ApiTests.Procurement;

/// <summary>Dev guide §14 / spec §30's submit -> approve -> PO -> receipt flow, plus the approval-workflow guards this module has been the eventual target for since M2's stand-in self-suspend test.</summary>
public class ProcurementFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullFlow_SubmitApprovePurchaseOrderReceipt_Succeeds()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(adminClient);

        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");
        var (managerClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "manager");

        var createResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 10, EstimatedUnitPrice = 5m }],
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        Assert.Equal(50m, request.EstimatedValue);
        Assert.Equal("Pending", request.Status);

        var approveResponse = await managerClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = (await approveResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        Assert.Equal("Approved", approved.Status);

        var poResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 10, UnitPrice = 6m }],
        });
        Assert.Equal(HttpStatusCode.Created, poResponse.StatusCode);
        var order = (await poResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;
        Assert.Equal(60m, order.Total);
        Assert.Equal("Open", order.Status);

        var receiptResponse = await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            PurchaseOrderId = order.Id,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = 10 }],
        });
        Assert.Equal(HttpStatusCode.Created, receiptResponse.StatusCode);

        var balanceResponse = await adminClient.GetAsync($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        balanceResponse.EnsureSuccessStatusCode();
        var page = (await balanceResponse.Content.ReadFromJsonAsync<PagedResult<StockBalanceResponse>>())!;
        Assert.Equal(10, page.Items.Single().QuantityOnHand);
    }

    [Fact]
    public async Task Approve_OwnPurchaseRequest_Denied()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;

        var approveResponse = await client.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());

        Assert.Equal(HttpStatusCode.Forbidden, approveResponse.StatusCode);
    }

    [Fact]
    public async Task Approve_AboveThreshold_RequiresProcurementApproveLarge()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");
        var (managerClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "manager");

        // Default threshold is 10000 - well above it.
        var createResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Large restock",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 200, EstimatedUnitPrice = 100m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        Assert.Equal(20000m, request.EstimatedValue);

        // manager holds procurement.approve but not procurement.approve.large.
        var deniedResponse = await managerClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        // The tenant-admin holds procurement.approve.large and isn't the requester.
        var allowedResponse = await adminClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task Reject_IsTerminal_CannotLaterBeApproved()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");
        var (managerClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "manager");

        var createResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;

        var rejectResponse = await managerClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/reject", new RejectPurchaseRequestRequest { Notes = "Not needed" });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        Assert.Equal("Rejected", (await rejectResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!.Status);

        var reapproveResponse = await managerClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest());
        Assert.Equal(HttpStatusCode.Conflict, reapproveResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_FromNonApprovedRequest_Returns409()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(client);
        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;

        var poResponse = await client.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });

        Assert.Equal(HttpStatusCode.Conflict, poResponse.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_RequestAlreadyConverted_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, _, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var supplierId = await ProcurementTestHelpers.CreateSupplierAsync(adminClient);
        var (requesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");

        var createResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();

        var firstPoRequest = new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        };
        var firstResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-orders", firstPoRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierId,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GoodsReceipt_AgainstAnotherTenantsPurchaseOrder_Returns404()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var (branchIdA, _, productIdA) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(tenantAClient);
        var supplierIdA = await ProcurementTestHelpers.CreateSupplierAsync(tenantAClient);

        var (otherRequesterClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(tenantAClient, factory, "procurement");
        var createResponse = await otherRequesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchIdA,
            Justification = "Restock widgets",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productIdA, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        // The tenant admin approves - approving one's own request is denied (Approve_OwnPurchaseRequest_Denied covers that).
        (await tenantAClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();

        var poResponse = await otherRequesterClient.PostAsJsonAsync("/api/v1/purchase-orders", new CreatePurchaseOrderRequest
        {
            PurchaseRequestId = request.Id,
            SupplierId = supplierIdA,
            PoNumber = "PO-" + Guid.NewGuid().ToString("N")[..8],
            Lines = [new CreatePurchaseOrderLine { ProductId = productIdA, Quantity = 1, UnitPrice = 1m }],
        });
        var orderA = (await poResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>())!;

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);
        var (_, warehouseIdB, productIdB) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(tenantBClient);

        var receiptResponse = await tenantBClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseIdB,
            PurchaseOrderId = orderA.Id,
            Lines = [new ReceiveStockLine { ProductId = productIdB, Quantity = 1 }],
        });

        Assert.Equal(HttpStatusCode.NotFound, receiptResponse.StatusCode);
    }
}
