using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.MasterData;
using EBOSP.Contracts.Sales;

namespace EBOSP.ApiTests.Sales;

/// <summary>Dev guide §15's Quotation -> customer acceptance -> Sales Order -> stock reservation -> Delivery flow, plus the credit-limit and compensation guards this module introduces.</summary>
public class SalesFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullFlow_QuoteAcceptOrderDeliver_Succeeds()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        await ReceiveAsync(adminClient, warehouseId, productId, 100);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient);

        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        var quoteResponse = await salesClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 10, UnitPrice = 5m }],
        });
        Assert.Equal(HttpStatusCode.Created, quoteResponse.StatusCode);
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        Assert.Equal(50m, quotation.Total);

        var acceptResponse = await salesClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal("Accepted", (await acceptResponse.Content.ReadFromJsonAsync<QuotationResponse>())!.Status);

        var orderResponse = await salesClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = (await orderResponse.Content.ReadFromJsonAsync<SalesOrderResponse>())!;
        Assert.Equal(50m, order.Total);
        Assert.Equal("Open", order.Status);

        // Reservation reduces available, not on-hand.
        var midBalance = await GetBalanceAsync(adminClient, warehouseId, productId);
        Assert.Equal(100, midBalance.QuantityOnHand);
        Assert.Equal(10, midBalance.QuantityReserved);
        Assert.Equal(90, midBalance.QuantityAvailable);

        var deliverResponse = await salesClient.PostAsJsonAsync("/api/v1/deliveries", new CreateDeliveryRequest { SalesOrderId = order.Id });
        Assert.Equal(HttpStatusCode.Created, deliverResponse.StatusCode);

        var finalBalance = await GetBalanceAsync(adminClient, warehouseId, productId);
        Assert.Equal(90, finalBalance.QuantityOnHand);
        Assert.Equal(0, finalBalance.QuantityReserved);
        Assert.Equal(90, finalBalance.QuantityAvailable);

        var orderAfterDelivery = await salesClient.GetAsync($"/api/v1/sales-orders/{order.Id}");
        Assert.Equal("Fulfilled", (await orderAfterDelivery.Content.ReadFromJsonAsync<SalesOrderResponse>())!.Status);
    }

    [Fact]
    public async Task CreateSalesOrder_ExceedsCreditLimit_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        await ReceiveAsync(adminClient, warehouseId, productId, 100);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient, creditLimit: 40m);
        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        var quoteResponse = await salesClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 10, UnitPrice = 5m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        Assert.Equal(50m, quotation.Total);
        (await salesClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();

        var orderResponse = await salesClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });

        Assert.Equal(HttpStatusCode.Conflict, orderResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_FromPendingQuotation_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient);

        var quoteResponse = await adminClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;

        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });

        Assert.Equal(HttpStatusCode.Conflict, orderResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_QuotationAlreadyConverted_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        await ReceiveAsync(adminClient, warehouseId, productId, 100);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient);

        var quoteResponse = await adminClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();

        var firstOrder = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        Assert.Equal(HttpStatusCode.Created, firstOrder.StatusCode);

        var secondOrder = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        Assert.Equal(HttpStatusCode.Conflict, secondOrder.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_InsufficientStockOnOneLine_Returns409AndReleasesEverythingElseReserved()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productAId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        var productBResponse = await adminClient.PostAsJsonAsync("/api/v1/products", new CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Product B " + Guid.NewGuid(),
            UnitPrice = 10m,
            TaxRatePercent = 0m,
            ReorderLevel = 0,
        });
        var productBId = (await productBResponse.Content.ReadFromJsonAsync<ProductResponse>())!.Id;

        // Plenty of A, hardly any B - the order below asks for more B than is available.
        await ReceiveAsync(adminClient, warehouseId, productAId, 100);
        await ReceiveAsync(adminClient, warehouseId, productBId, 5);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient);

        var quoteResponse = await adminClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines =
            [
                new CreateQuotationLine { ProductId = productAId, Quantity = 10, UnitPrice = 1m },
                new CreateQuotationLine { ProductId = productBId, Quantity = 10, UnitPrice = 1m },
            ],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();

        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        Assert.Equal(HttpStatusCode.Conflict, orderResponse.StatusCode);

        // Whichever line reserved successfully before the failure must have been released again -
        // no orphaned reservation left behind on either product.
        var balanceA = await GetBalanceAsync(adminClient, warehouseId, productAId);
        var balanceB = await GetBalanceAsync(adminClient, warehouseId, productBId);
        Assert.Equal(0, balanceA.QuantityReserved);
        Assert.Equal(0, balanceB.QuantityReserved);
        Assert.Equal(100, balanceA.QuantityAvailable);
        Assert.Equal(5, balanceB.QuantityAvailable);
    }

    [Fact]
    public async Task CreateDelivery_AgainstNonOpenOrder_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        await ReceiveAsync(adminClient, warehouseId, productId, 100);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(adminClient);

        var quoteResponse = await adminClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();
        var orderResponse = await adminClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        var order = (await orderResponse.Content.ReadFromJsonAsync<SalesOrderResponse>())!;

        (await adminClient.PostAsJsonAsync("/api/v1/deliveries", new CreateDeliveryRequest { SalesOrderId = order.Id })).EnsureSuccessStatusCode();
        var secondDelivery = await adminClient.PostAsJsonAsync("/api/v1/deliveries", new CreateDeliveryRequest { SalesOrderId = order.Id });

        Assert.Equal(HttpStatusCode.Conflict, secondDelivery.StatusCode);
    }

    [Fact]
    public async Task CreateDelivery_AgainstAnotherTenantsSalesOrder_Returns404()
    {
        var tenantAClient = factory.CreateClient();
        var tenantA = await AuthTestHelpers.CreateTenantAdminAsync(tenantAClient);
        AuthTestHelpers.AuthorizeAs(tenantAClient, tenantA.Tokens);
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(tenantAClient);
        await ReceiveAsync(tenantAClient, warehouseId, productId, 100);
        var customerId = await SalesTestHelpers.CreateCustomerAsync(tenantAClient);

        var quoteResponse = await tenantAClient.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = 1, UnitPrice = 1m }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        (await tenantAClient.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();
        var orderResponse = await tenantAClient.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        var order = (await orderResponse.Content.ReadFromJsonAsync<SalesOrderResponse>())!;

        var tenantBClient = factory.CreateClient();
        var tenantB = await AuthTestHelpers.CreateTenantAdminAsync(tenantBClient);
        AuthTestHelpers.AuthorizeAs(tenantBClient, tenantB.Tokens);

        var deliveryResponse = await tenantBClient.PostAsJsonAsync("/api/v1/deliveries", new CreateDeliveryRequest { SalesOrderId = order.Id });

        Assert.Equal(HttpStatusCode.NotFound, deliveryResponse.StatusCode);
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

    private static async Task<StockBalanceResponse> GetBalanceAsync(HttpClient client, Guid warehouseId, Guid productId)
    {
        var response = await client.GetAsync($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        response.EnsureSuccessStatusCode();
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<StockBalanceResponse>>())!;
        return page.Items.Single();
    }
}
