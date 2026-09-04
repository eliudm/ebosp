using EBOSP.Application.Common;
using EBOSP.Application.Reporting;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Reporting;
using EBOSP.Domain.Identity;
using EBOSP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EBOSP.Infrastructure.Reporting;

/// <summary>Read-only aggregations over the operational tables (spec §19: "simple reports may query read-optimized projections" - no separate reporting store at this scale).</summary>
public sealed class ReportingRepository(AppDbContext context, IClock clock) : IReportingRepository
{
    public async Task<SalesReportResponse> GetSalesReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var ordersQuery = context.SalesOrders.Where(o => o.TenantId == tenantId && o.CreatedAt >= from && o.CreatedAt <= to);
        var totalOrders = await ordersQuery.CountAsync(cancellationToken);
        var totalOrderValue = await ordersQuery.SumAsync(o => o.Total, cancellationToken);

        var invoicesQuery = context.Invoices.Where(i => i.TenantId == tenantId && i.CreatedAt >= from && i.CreatedAt <= to);
        var totalInvoices = await invoicesQuery.CountAsync(cancellationToken);
        var totalInvoiced = await invoicesQuery.SumAsync(i => i.Total, cancellationToken);

        var totalCollected = await context.Payments
            .Where(p => p.TenantId == tenantId && p.Status == PaymentStatus.Successful && p.ConfirmedAt >= from && p.ConfirmedAt <= to)
            .SumAsync(p => p.Amount, cancellationToken);

        return new SalesReportResponse(from, to, totalOrders, totalOrderValue, totalInvoices, totalInvoiced, totalCollected, totalInvoiced - totalCollected);
    }

    public async Task<InventoryReportResponse> GetInventoryReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var totalValuation = await context.StockBalances
            .Where(b => b.TenantId == tenantId)
            .Join(context.Products.Where(p => p.TenantId == tenantId), b => b.ProductId, p => p.Id, (b, p) => b.QuantityOnHand * p.UnitPrice)
            .SumAsync(cancellationToken);

        var ledgerQuery = context.StockLedgerEntries.Where(e => e.TenantId == tenantId && e.OccurredAt >= from && e.OccurredAt <= to);
        var totalReceived = await ledgerQuery.Where(e => e.EventType == StockLedgerEventType.Received).SumAsync(e => e.Quantity, cancellationToken);
        // Issued/TransferOut are stored as negative quantities (InventoryService) - negate so the
        // report reads as "how much moved", not a signed ledger delta.
        var totalIssued = await ledgerQuery.Where(e => e.EventType == StockLedgerEventType.Issued).SumAsync(e => -e.Quantity, cancellationToken);
        var totalAdjustedNet = await ledgerQuery.Where(e => e.EventType == StockLedgerEventType.Adjusted).SumAsync(e => e.Quantity, cancellationToken);
        var totalTransferred = await ledgerQuery.Where(e => e.EventType == StockLedgerEventType.TransferIn).SumAsync(e => e.Quantity, cancellationToken);

        return new InventoryReportResponse(totalValuation, from, to, totalReceived, totalIssued, totalAdjustedNet, totalTransferred);
    }

    public async Task<PagedResult<LowStockReportItem>> GetLowStockAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var balances = context.StockBalances.Where(b => b.TenantId == tenantId);
        var rules = context.ReorderRules.Where(r => r.TenantId == tenantId);
        var products = context.Products.Where(p => p.TenantId == tenantId);

        var query =
            from b in balances
            join r in rules on new { b.WarehouseId, b.ProductId } equals new { r.WarehouseId, r.ProductId } into ruleGroup
            from r in ruleGroup.DefaultIfEmpty()
            join p in products on b.ProductId equals p.Id
            let effectiveLevel = r != null ? r.ReorderLevel : p.ReorderLevel
            where effectiveLevel > 0 && b.QuantityOnHand < effectiveLevel
            select new { b.WarehouseId, b.ProductId, b.QuantityOnHand, EffectiveLevel = effectiveLevel };

        var totalCount = await query.CountAsync(cancellationToken);
        // Sort on the anonymous projection, not the LowStockReportItem record - EF Core can't
        // translate ordering by a property read back off an already-constructed record type.
        var ordered = request.SortDescending ? query.OrderByDescending(i => i.QuantityOnHand) : query.OrderBy(i => i.QuantityOnHand);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new LowStockReportItem(i.WarehouseId, i.ProductId, i.QuantityOnHand, i.EffectiveLevel))
            .ToListAsync(cancellationToken);

        return new PagedResult<LowStockReportItem> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public async Task<PagedResult<SlowMovingInventoryItem>> GetSlowMovingInventoryAsync(Guid tenantId, int daysInactive, PagedRequest request, CancellationToken cancellationToken)
    {
        var cutoff = clock.UtcNow.AddDays(-daysInactive);
        var balances = context.StockBalances.Where(b => b.TenantId == tenantId);

        var withLastMovement = balances.Select(b => new
        {
            b.WarehouseId,
            b.ProductId,
            b.QuantityOnHand,
            LastMovementAt = context.StockLedgerEntries
                .Where(e => e.TenantId == tenantId && e.WarehouseId == b.WarehouseId && e.ProductId == b.ProductId)
                .Max(e => (DateTimeOffset?)e.OccurredAt),
        });

        var slowMoving = withLastMovement.Where(x => x.LastMovementAt == null || x.LastMovementAt < cutoff);

        var totalCount = await slowMoving.CountAsync(cancellationToken);
        var ordered = request.SortDescending
            ? slowMoving.OrderByDescending(x => x.LastMovementAt ?? DateTimeOffset.MinValue)
            : slowMoving.OrderBy(x => x.LastMovementAt ?? DateTimeOffset.MinValue);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SlowMovingInventoryItem(x.WarehouseId, x.ProductId, x.QuantityOnHand, x.LastMovementAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SlowMovingInventoryItem> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public async Task<ProcurementSpendReportResponse> GetProcurementSpendReportAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var ordersQuery = context.PurchaseOrders.Where(o => o.TenantId == tenantId && o.CreatedAt >= from && o.CreatedAt <= to);
        var totalPurchaseOrders = await ordersQuery.CountAsync(cancellationToken);
        var totalSpend = await ordersQuery.SumAsync(o => o.Total, cancellationToken);

        var bySupplier = await ordersQuery
            .GroupBy(o => o.SupplierId)
            .Select(g => new { SupplierId = g.Key, Total = g.Sum(o => o.Total) })
            .OrderByDescending(x => x.Total)
            .Take(20)
            .ToListAsync(cancellationToken);

        var supplierIds = bySupplier.Select(x => x.SupplierId).ToList();
        var supplierNames = await context.Suppliers
            .Where(s => s.TenantId == tenantId && supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var bySupplierResult = bySupplier
            .Select(x => new ProcurementSpendBySupplier(x.SupplierId, supplierNames.GetValueOrDefault(x.SupplierId, "Unknown"), x.Total))
            .ToList();

        return new ProcurementSpendReportResponse(from, to, totalPurchaseOrders, totalSpend, bySupplierResult);
    }

    public async Task<PagedResult<OutstandingInvoiceItem>> GetOutstandingInvoicesAsync(Guid tenantId, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = context.Invoices.Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Issued);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = request.SortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt);
        var items = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new OutstandingInvoiceItem(i.Id, i.CustomerId, i.Total, i.PaidTotal, i.Total - i.PaidTotal, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<OutstandingInvoiceItem> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = totalCount };
    }

    public async Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(Guid tenantId, DateTimeOffset from, DateTimeOffset to, int top, CancellationToken cancellationToken)
    {
        var linesInRange = context.SalesOrderLines
            .Where(l => l.TenantId == tenantId)
            .Join(
                context.SalesOrders.Where(o => o.TenantId == tenantId && o.CreatedAt >= from && o.CreatedAt <= to),
                l => l.SalesOrderId,
                o => o.Id,
                (l, o) => l);

        var grouped = await linesInRange
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, QuantitySold = g.Sum(l => l.Quantity), Revenue = g.Sum(l => l.Quantity * l.UnitPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(top)
            .ToListAsync(cancellationToken);

        var productIds = grouped.Select(x => x.ProductId).ToList();
        var productNames = await context.Products
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return grouped
            .Select(x => new TopProductItem(x.ProductId, productNames.GetValueOrDefault(x.ProductId, "Unknown"), x.QuantitySold, x.Revenue))
            .ToList();
    }
}
