using EBOSP.Application.Notifications;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Notifications;

public sealed class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken) =>
        await context.Notifications.AddAsync(notification, cancellationToken);

    public Task<Notification?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Notifications.SingleOrDefaultAsync(n => n.TenantId == tenantId && n.Id == id, cancellationToken);

    public async Task<PagedResult<Notification>> ListForRecipientAsync(Guid tenantId, Guid recipientUserId, bool unreadOnly, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Notifications.Where(n => n.TenantId == tenantId && n.RecipientUserId == recipientUserId);
        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(n => n.CreatedAt) : query.OrderBy(n => n.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Notification> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }
}
