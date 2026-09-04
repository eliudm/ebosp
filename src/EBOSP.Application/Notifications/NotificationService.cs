using EBOSP.Application.Common;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Notifications;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Notifications;

public sealed class NotificationService(INotificationRepository notifications, IUnitOfWork unitOfWork, IClock clock) : INotificationService
{
    public async Task<PagedResult<NotificationResponse>> ListMineAsync(Guid tenantId, Guid recipientUserId, bool unreadOnly, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await notifications.ListForRecipientAsync(tenantId, recipientUserId, unreadOnly, request, cancellationToken);
        return new PagedResult<NotificationResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<NotificationResponse> MarkReadAsync(Guid tenantId, Guid recipientUserId, Guid id, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(tenantId, id, cancellationToken)
                            ?? throw new NotFoundException("Notification not found.");

        // Not the recipient's own notification - 404, not 403, so its existence isn't leaked to
        // another tenant user (same "don't leak existence" shape used everywhere else).
        if (notification.RecipientUserId != recipientUserId)
        {
            throw new NotFoundException("Notification not found.");
        }

        if (notification.ReadAt is not null)
        {
            throw new ConflictException("This notification is already marked read.");
        }

        notification.MarkRead(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(notification);
    }

    private static NotificationResponse ToResponse(Notification notification) => new(
        notification.Id, notification.Title, notification.Body, notification.RelatedAggregateType, notification.RelatedAggregateId, notification.CreatedAt, notification.ReadAt);
}
