using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Contracts.Security;

namespace EBOSP.E2ETests.Journeys;

/// <summary>Dev guide §25.3 "Inventory adjustment -> security alert" - a large adjustment must surface as an investigable, system-raised alert, not just an audit log line.</summary>
public class InventoryAdjustmentToAlertTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FullJourney_LargeAdjustment_RaisesAndListsASecurityAlert()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (_, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        // Default LargeAdjustmentThreshold is 100 - well above it.
        var adjustResponse = await adminClient.PostAsJsonAsync("/api/v1/inventory/adjustments", new AdjustStockRequest
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityDelta = 250,
            Reason = "E2E journey - cycle count correction",
        });
        Assert.Equal(HttpStatusCode.Created, adjustResponse.StatusCode);

        var alertsResponse = await adminClient.GetAsync("/api/v1/security/alerts");
        Assert.Equal(HttpStatusCode.OK, alertsResponse.StatusCode);
        var alerts = (await alertsResponse.Content.ReadFromJsonAsync<PagedResult<SecurityAlertResponse>>())!;
        var alert = Assert.Single(alerts.Items, a => a.Rule == "LargeStockAdjustment");
        Assert.Equal("Open", alert.Status);
    }
}
