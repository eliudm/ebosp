using EBOSP.Application.Common;
using EBOSP.Application.Notifications;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>A user's own in-app notification inbox (dev guide §20) - no permission beyond authentication, since every user only ever sees their own.</summary>
[Authorize]
public sealed class NotificationsController(INotificationService notificationService, ICurrentUserContext currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationResponse>>> List([FromQuery] bool unreadOnly, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await notificationService.ListMineAsync(TenantId, UserId, unreadOnly, request, cancellationToken));

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Ok(await notificationService.MarkReadAsync(TenantId, UserId, id, cancellationToken));

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");

    private Guid UserId => currentUser.UserId ?? throw new InvalidOperationException("Authenticated request is missing a user claim.");
}
