using System.Net.Http.Json;
using EBOSP.ApiTests.Sales;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Sales;

namespace EBOSP.ApiTests.Billing;

internal static class BillingTestHelpers
{
    /// <summary>Receives enough stock, then walks a customer/quotation/order all the way through delivery - returns the resulting Fulfilled sales order's id, customer id and total.</summary>
    public static async Task<(Guid CustomerId, Guid OrderId, decimal Total)> CreateFulfilledSalesOrderAsync(
        HttpClient client, Guid branchId, Guid warehouseId, Guid productId, int quantity = 10, decimal unitPrice = 5m)
    {
        var receiveResponse = await client.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = productId, Quantity = quantity }],
        });
        receiveResponse.EnsureSuccessStatusCode();

        var customerId = await SalesTestHelpers.CreateCustomerAsync(client);

        var quoteResponse = await client.PostAsJsonAsync("/api/v1/quotations", new CreateQuotationRequest
        {
            CustomerId = customerId,
            BranchId = branchId,
            Lines = [new CreateQuotationLine { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice }],
        });
        var quotation = (await quoteResponse.Content.ReadFromJsonAsync<QuotationResponse>())!;
        (await client.PostAsJsonAsync($"/api/v1/quotations/{quotation.Id}/accept", (object?)null)).EnsureSuccessStatusCode();

        var orderResponse = await client.PostAsJsonAsync("/api/v1/sales-orders", new CreateSalesOrderRequest { QuotationId = quotation.Id, WarehouseId = warehouseId });
        var order = (await orderResponse.Content.ReadFromJsonAsync<SalesOrderResponse>())!;

        (await client.PostAsJsonAsync("/api/v1/deliveries", new CreateDeliveryRequest { SalesOrderId = order.Id })).EnsureSuccessStatusCode();

        return (customerId, order.Id, order.Total);
    }
}
