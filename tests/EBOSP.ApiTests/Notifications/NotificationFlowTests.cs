using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Identity;
using EBOSP.Contracts.Notifications;
using EBOSP.Contracts.Procurement;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EBOSP.ApiTests.Notifications;

/// <summary>
/// The API-visible half of dev guide §20's notification pipeline. The other half (the outbox
/// background processor actually creating the Notification row) is covered directly against real
/// Postgres by EBOSP.IntegrationTests.Notifications.NotificationOutboxProcessorTests - these tests
/// seed a Notification directly (as the processor would have produced) to exercise the read/API
/// surface in isolation, since CustomWebApplicationFactory doesn't run the separate Worker process.
/// </summary>
public class NotificationFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ListMine_OnlyReturnsOwnNotifications()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (requesterClient, requesterId) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");
        var (branchId, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);

        var createResponse = await requesterClient.PostAsJsonAsync("/api/v1/purchase-requests", new CreatePurchaseRequestRequest
        {
            BranchId = branchId,
            Justification = "Restock",
            RequiredDate = DateTimeOffset.UtcNow.AddDays(7),
            Lines = [new CreatePurchaseRequestLine { ProductId = productId, Quantity = 1, EstimatedUnitPrice = 1m }],
        });
        var request = (await createResponse.Content.ReadFromJsonAsync<PurchaseRequestResponse>())!;
        (await adminClient.PostAsJsonAsync($"/api/v1/purchase-requests/{request.Id}/approve", new ApprovePurchaseRequestRequest())).EnsureSuccessStatusCode();

        // The outbox row now carries NotifyUserId=requesterId - proven directly by
        // NotificationOutboxProcessorTests. Seed the Notification a processor run would have
        // produced, so this test exercises the read/ownership API surface without needing the
        // separate Worker process running inside this test host.
        await SeedNotificationAsync(requesterId, "Your purchase request was approved");

        var mineResponse = await requesterClient.GetAsync("/api/v1/notifications");
        mineResponse.EnsureSuccessStatusCode();
        var mine = (await mineResponse.Content.ReadFromJsonAsync<PagedResult<NotificationResponse>>())!;
        Assert.Contains(mine.Items, n => n.Title == "Your purchase request was approved");

        // The approver (admin) never had a notification created for them - they're not the recipient.
        var adminMineResponse = await adminClient.GetAsync("/api/v1/notifications");
        var adminMine = (await adminMineResponse.Content.ReadFromJsonAsync<PagedResult<NotificationResponse>>())!;
        Assert.DoesNotContain(adminMine.Items, n => n.Title == "Your purchase request was approved");
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Succeeds()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        var listResponse = await adminClient.GetAsync("/api/v1/notifications");
        var before = (await listResponse.Content.ReadFromJsonAsync<PagedResult<NotificationResponse>>())!;
        Assert.Empty(before.Items);
    }

    [Fact]
    public async Task MarkRead_AlreadyRead_Returns409()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var notificationId = await SeedNotificationAsync(admin.AdminUserId, "Test");

        (await adminClient.PostAsync($"/api/v1/notifications/{notificationId}/read", content: null)).EnsureSuccessStatusCode();
        var secondAttempt = await adminClient.PostAsync($"/api/v1/notifications/{notificationId}/read", content: null);

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task MarkRead_AnotherUsersNotification_Returns404()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (otherClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "procurement");
        var notificationId = await SeedNotificationAsync(admin.AdminUserId, "Admin only");

        var response = await otherClient.PostAsync($"/api/v1/notifications/{notificationId}/read", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SeedNotificationAsync(Guid recipientUserId, string title)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipient = await context.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == recipientUserId);
        var notification = Notification.Create(
            recipient.TenantId, recipientUserId, title, null, null, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return notification.Id;
    }
}
