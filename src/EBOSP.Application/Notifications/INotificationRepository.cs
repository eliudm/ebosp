using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Notification>> ListForRecipientAsync(Guid tenantId, Guid recipientUserId, bool unreadOnly, PagedRequest request, CancellationToken cancellationToken);
}
