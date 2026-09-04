using EBOSP.Application.Common;
using EBOSP.Application.Reporting;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Read-only aggregations over data every prior module already owns (spec §8.6/§19) - gated by
/// [Authorize] only, no new permission: every report summarizes records a user could already see
/// in detail via the underlying module's own (plain-[Authorize]) read endpoints.
/// </summary>
[Authorize]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(IReportingService reports, ICurrentUserContext currentUser, IClock clock) : ApiControllerBase
{
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportResponse>> Sales([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveRange(from, to);
        return Ok(await reports.GetSalesReportAsync(TenantId, resolvedFrom, resolvedTo, cancellationToken));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportResponse>> Inventory([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveRange(from, to);
        return Ok(await reports.GetInventoryReportAsync(TenantId, resolvedFrom, resolvedTo, cancellationToken));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<PagedResult<LowStockReportItem>>> LowStock([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await reports.GetLowStockAsync(TenantId, request, cancellationToken));

    [HttpGet("slow-moving-inventory")]
    public async Task<ActionResult<PagedResult<SlowMovingInventoryItem>>> SlowMovingInventory(
        [FromQuery] int daysInactive, [FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        // Clamped, not just defaulted below zero - an unclamped upper bound (e.g. int.MaxValue)
        // would overflow DateTimeOffset.AddDays and throw an unhandled 500.
        Ok(await reports.GetSlowMovingInventoryAsync(TenantId, Math.Clamp(daysInactive <= 0 ? 90 : daysInactive, 1, 3650), request, cancellationToken));

    [HttpGet("procurement-spend")]
    public async Task<ActionResult<ProcurementSpendReportResponse>> ProcurementSpend([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveRange(from, to);
        return Ok(await reports.GetProcurementSpendReportAsync(TenantId, resolvedFrom, resolvedTo, cancellationToken));
    }

    [HttpGet("outstanding-invoices")]
    public async Task<ActionResult<PagedResult<OutstandingInvoiceItem>>> OutstandingInvoices([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(await reports.GetOutstandingInvoicesAsync(TenantId, request, cancellationToken));

    private (DateTimeOffset From, DateTimeOffset To) ResolveRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        var now = clock.UtcNow;
        return (from ?? now.AddDays(-30), to ?? now);
    }

    private Guid TenantId => currentUser.TenantId ?? throw new InvalidOperationException("Authenticated request is missing a tenant claim.");
}
