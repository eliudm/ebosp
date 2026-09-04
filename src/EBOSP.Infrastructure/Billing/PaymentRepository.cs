using EBOSP.Application.Billing;
using EBOSP.Contracts.Common;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Billing;

public sealed class PaymentRepository(AppDbContext context) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        await context.Payments.AddAsync(payment, cancellationToken);

    public Task<Payment?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken) =>
        context.Payments.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Id == id, cancellationToken);

    public async Task<PagedResult<Payment>> ListAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Payments.Where(p => p.TenantId == tenantId);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
        var items = await ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return new PagedResult<Payment> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public Task<bool> IdempotencyKeyExistsAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
        context.Payments.AnyAsync(p => p.TenantId == tenantId && p.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<decimal> SumSuccessfulAmountForInvoiceAsync(Guid tenantId, Guid invoiceId, CancellationToken cancellationToken) =>
        context.Payments
            .Where(p => p.TenantId == tenantId && p.InvoiceId == invoiceId && p.Status == PaymentStatus.Successful)
            .SumAsync(p => p.Amount, cancellationToken);
}
