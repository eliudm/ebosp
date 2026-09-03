using EBOSP.Application.Common;
using EBOSP.Application.MasterData;
using EBOSP.Contracts.Common;
using EBOSP.Contracts.Inventory;
using EBOSP.Domain.Identity;

namespace EBOSP.Application.Inventory;

/// <summary>Implements the movement pipeline from dev guide §13.3: validate → mutate balance → write ledger → write outbox event.</summary>
public sealed class InventoryService(
    IStockBalanceRepository stockBalances,
    IStockLedgerEntryRepository ledgerEntries,
    IGoodsReceiptRepository goodsReceipts,
    IStockAdjustmentRepository stockAdjustments,
    IStockTransferRepository stockTransfers,
    IStockReservationRepository reservations,
    IReorderRuleRepository reorderRules,
    IWarehouseRepository warehouses,
    IProductRepository products,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    InventoryOptions options) : IInventoryService
{
    public async Task<GoodsReceiptResponse> ReceiveAsync(Guid tenantId, Guid actingUserId, ReceiveStockRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key && await goodsReceipts.IdempotencyKeyExistsAsync(tenantId, key, cancellationToken))
        {
            throw new ConflictException("This receipt has already been processed.");
        }

        await EnsureWarehouseOwnedAsync(tenantId, request.WarehouseId, cancellationToken);
        foreach (var line in request.Lines)
        {
            await EnsureProductOwnedAsync(tenantId, line.ProductId, cancellationToken);
        }

        var now = clock.UtcNow;
        var receipt = GoodsReceipt.Create(
            tenantId, request.WarehouseId, actingUserId, now, request.PurchaseOrderId, request.Reference, request.IdempotencyKey,
            request.Lines.Select(l => (l.ProductId, l.Quantity)).ToList());
        await goodsReceipts.AddAsync(receipt, cancellationToken);

        var lines = new List<(StockBalance Balance, int Quantity)>();
        foreach (var line in request.Lines)
        {
            var balance = await GetOrCreateBalanceAsync(tenantId, request.WarehouseId, line.ProductId, cancellationToken);
            lines.Add((balance, line.Quantity));
            await ledgerEntries.AddAsync(
                StockLedgerEntry.Create(tenantId, request.WarehouseId, line.ProductId, StockLedgerEventType.Received, line.Quantity, actingUserId, now, nameof(GoodsReceipt), receipt.Id, null, request.IdempotencyKey),
                cancellationToken);
        }

        events.Record("StockReceived", tenantId, nameof(GoodsReceipt), receipt.Id.ToString(), new { request.WarehouseId, request.Lines }, actorId: actingUserId);

        var before = new int[lines.Count];
        await SaveWithConcurrencyRetryAsync(
            lines.Select(l => l.Balance).ToList(),
            () =>
            {
                for (var i = 0; i < lines.Count; i++)
                {
                    before[i] = lines[i].Balance.QuantityOnHand;
                    lines[i].Balance.Receive(lines[i].Quantity);
                }
            },
            cancellationToken);

        for (var i = 0; i < lines.Count; i++)
        {
            await RaiseLowStockIfCrossedAsync(tenantId, request.WarehouseId, lines[i].Balance.ProductId, before[i], lines[i].Balance.QuantityOnHand, cancellationToken);
        }

        return ToResponse(receipt);
    }

    public async Task<StockLedgerEntryResponse> IssueAsync(Guid tenantId, Guid actingUserId, IssueStockRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key && await ledgerEntries.IdempotencyKeyExistsAsync(tenantId, key, cancellationToken))
        {
            throw new ConflictException("This issue has already been processed.");
        }

        await EnsureWarehouseOwnedAsync(tenantId, request.WarehouseId, cancellationToken);
        await EnsureProductOwnedAsync(tenantId, request.ProductId, cancellationToken);

        var now = clock.UtcNow;
        var balance = await GetOrCreateBalanceAsync(tenantId, request.WarehouseId, request.ProductId, cancellationToken);

        var entry = StockLedgerEntry.Create(tenantId, request.WarehouseId, request.ProductId, StockLedgerEventType.Issued, -request.Quantity, actingUserId, now, null, null, request.Reason, request.IdempotencyKey);
        await ledgerEntries.AddAsync(entry, cancellationToken);

        events.Record("StockIssued", tenantId, nameof(Product), request.ProductId.ToString(), new { request.WarehouseId, request.Quantity }, actorId: actingUserId);

        var before = 0;
        await SaveWithConcurrencyRetryAsync([balance], () =>
        {
            before = balance.QuantityOnHand;
            balance.Issue(request.Quantity);
        }, cancellationToken);

        await RaiseLowStockIfCrossedAsync(tenantId, request.WarehouseId, request.ProductId, before, balance.QuantityOnHand, cancellationToken);

        return ToResponse(entry);
    }

    public async Task<StockAdjustmentResponse> AdjustAsync(Guid tenantId, Guid actingUserId, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key && await stockAdjustments.IdempotencyKeyExistsAsync(tenantId, key, cancellationToken))
        {
            throw new ConflictException("This adjustment has already been processed.");
        }

        await EnsureWarehouseOwnedAsync(tenantId, request.WarehouseId, cancellationToken);
        await EnsureProductOwnedAsync(tenantId, request.ProductId, cancellationToken);

        var now = clock.UtcNow;
        var adjustment = StockAdjustment.Create(tenantId, request.WarehouseId, request.ProductId, request.QuantityDelta, request.Reason, actingUserId, now, request.IdempotencyKey);
        await stockAdjustments.AddAsync(adjustment, cancellationToken);

        var balance = await GetOrCreateBalanceAsync(tenantId, request.WarehouseId, request.ProductId, cancellationToken);
        await ledgerEntries.AddAsync(
            StockLedgerEntry.Create(tenantId, request.WarehouseId, request.ProductId, StockLedgerEventType.Adjusted, request.QuantityDelta, actingUserId, now, nameof(StockAdjustment), adjustment.Id, request.Reason, request.IdempotencyKey),
            cancellationToken);

        events.Record("StockAdjusted", tenantId, nameof(StockAdjustment), adjustment.Id.ToString(), new { request.WarehouseId, request.ProductId, request.QuantityDelta, request.Reason }, actorId: actingUserId);

        // The caller-side permission check (inventory.adjust.large) already happened before this
        // method was invoked - this is the corresponding "+ alert" half of spec §16's "Manager
        // approval + alert" response.
        if (Math.Abs(request.QuantityDelta) > options.LargeAdjustmentThreshold)
        {
            events.Record("StockAdjustmentFlagged", tenantId, nameof(StockAdjustment), adjustment.Id.ToString(), new { request.WarehouseId, request.ProductId, request.QuantityDelta }, actorId: actingUserId);
        }

        var before = 0;
        await SaveWithConcurrencyRetryAsync([balance], () =>
        {
            before = balance.QuantityOnHand;
            balance.AdjustBy(request.QuantityDelta);
        }, cancellationToken);

        await RaiseLowStockIfCrossedAsync(tenantId, request.WarehouseId, request.ProductId, before, balance.QuantityOnHand, cancellationToken);

        return ToResponse(adjustment);
    }

    public async Task<StockTransferResponse> TransferAsync(Guid tenantId, Guid actingUserId, TransferStockRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key && await stockTransfers.IdempotencyKeyExistsAsync(tenantId, key, cancellationToken))
        {
            throw new ConflictException("This transfer has already been processed.");
        }

        await EnsureWarehouseOwnedAsync(tenantId, request.FromWarehouseId, cancellationToken);
        await EnsureWarehouseOwnedAsync(tenantId, request.ToWarehouseId, cancellationToken);
        await EnsureProductOwnedAsync(tenantId, request.ProductId, cancellationToken);

        var now = clock.UtcNow;
        var transfer = StockTransfer.Create(tenantId, request.FromWarehouseId, request.ToWarehouseId, request.ProductId, request.Quantity, request.Reason, actingUserId, now, request.IdempotencyKey);
        await stockTransfers.AddAsync(transfer, cancellationToken);

        var fromBalance = await GetOrCreateBalanceAsync(tenantId, request.FromWarehouseId, request.ProductId, cancellationToken);
        var toBalance = await GetOrCreateBalanceAsync(tenantId, request.ToWarehouseId, request.ProductId, cancellationToken);

        await ledgerEntries.AddAsync(
            StockLedgerEntry.Create(tenantId, request.FromWarehouseId, request.ProductId, StockLedgerEventType.TransferOut, -request.Quantity, actingUserId, now, nameof(StockTransfer), transfer.Id, request.Reason, request.IdempotencyKey),
            cancellationToken);
        await ledgerEntries.AddAsync(
            StockLedgerEntry.Create(tenantId, request.ToWarehouseId, request.ProductId, StockLedgerEventType.TransferIn, request.Quantity, actingUserId, now, nameof(StockTransfer), transfer.Id, request.Reason, request.IdempotencyKey),
            cancellationToken);

        // Not in spec §11's event catalogue (only the §10.3 figure narrative names it) - recorded
        // anyway since dev guide §13.2 requires "all stock changes emit domain events" without exception.
        events.Record("StockTransferred", tenantId, nameof(StockTransfer), transfer.Id.ToString(), new { request.FromWarehouseId, request.ToWarehouseId, request.ProductId, request.Quantity }, actorId: actingUserId);

        int fromBefore = 0, toBefore = 0;
        await SaveWithConcurrencyRetryAsync([fromBalance, toBalance], () =>
        {
            fromBefore = fromBalance.QuantityOnHand;
            toBefore = toBalance.QuantityOnHand;
            fromBalance.Issue(request.Quantity);
            toBalance.Receive(request.Quantity);
        }, cancellationToken);

        await RaiseLowStockIfCrossedAsync(tenantId, request.FromWarehouseId, request.ProductId, fromBefore, fromBalance.QuantityOnHand, cancellationToken);
        await RaiseLowStockIfCrossedAsync(tenantId, request.ToWarehouseId, request.ProductId, toBefore, toBalance.QuantityOnHand, cancellationToken);

        return ToResponse(transfer);
    }

    public async Task<StockReservationResponse> ReserveAsync(Guid tenantId, Guid actingUserId, ReserveStockRequest request, CancellationToken cancellationToken)
    {
        await EnsureWarehouseOwnedAsync(tenantId, request.WarehouseId, cancellationToken);
        await EnsureProductOwnedAsync(tenantId, request.ProductId, cancellationToken);

        var balance = await GetOrCreateBalanceAsync(tenantId, request.WarehouseId, request.ProductId, cancellationToken);
        var reservation = StockReservation.Create(tenantId, request.WarehouseId, request.ProductId, request.Quantity, clock.UtcNow);
        await reservations.AddAsync(reservation, cancellationToken);

        await SaveWithConcurrencyRetryAsync([balance], () => balance.Reserve(request.Quantity), cancellationToken);

        return ToResponse(reservation);
    }

    public async Task ReleaseReservationAsync(Guid tenantId, Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByIdAsync(tenantId, reservationId, cancellationToken)
                           ?? throw new NotFoundException("Reservation not found.");

        if (reservation.Status != StockReservationStatus.Active)
        {
            throw new ConflictException("Reservation is already released.");
        }

        var balance = await GetOrCreateBalanceAsync(tenantId, reservation.WarehouseId, reservation.ProductId, cancellationToken);

        // Marked released once, up front - not inside the retry delegate below, which can run more
        // than once per call and would otherwise double-release the same reservation's quantity
        // back onto the balance.
        reservation.Release(clock.UtcNow);

        await SaveWithConcurrencyRetryAsync([balance], () => balance.Release(reservation.Quantity), cancellationToken);
    }

    public async Task<PagedResult<StockBalanceResponse>> ListBalancesAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await stockBalances.ListAsync(tenantId, warehouseId, productId, request, cancellationToken);
        return new PagedResult<StockBalanceResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    public async Task<PagedResult<StockLedgerEntryResponse>> ListLedgerAsync(Guid tenantId, Guid? warehouseId, Guid? productId, PagedRequest request, CancellationToken cancellationToken)
    {
        var page = await ledgerEntries.ListAsync(tenantId, warehouseId, productId, request, cancellationToken);
        return new PagedResult<StockLedgerEntryResponse>
        {
            Items = page.Items.Select(ToResponse).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }

    private async Task EnsureWarehouseOwnedAsync(Guid tenantId, Guid warehouseId, CancellationToken cancellationToken) =>
        _ = await warehouses.GetByIdAsync(tenantId, warehouseId, cancellationToken) ?? throw new NotFoundException("Warehouse not found.");

    private async Task EnsureProductOwnedAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken) =>
        _ = await products.GetByIdAsync(tenantId, productId, cancellationToken) ?? throw new NotFoundException("Product not found.");

    /// <summary>
    /// Balances are created lazily on first movement. Two concurrent first-ever movements against
    /// the same (warehouse, product) pair could both attempt to insert - an accepted, narrow race
    /// left unhandled here (it would surface as a 500, not corrupted data: the unique index still
    /// guarantees only one row survives) since it's far rarer than the steady-state concurrent
    /// update case the retry machinery below specifically targets.
    /// </summary>
    private async Task<StockBalance> GetOrCreateBalanceAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken)
    {
        var existing = await stockBalances.GetAsync(tenantId, warehouseId, productId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = StockBalance.CreateEmpty(tenantId, warehouseId, productId);
        await stockBalances.AddAsync(created, cancellationToken);
        return created;
    }

    /// <summary>
    /// Applies <paramref name="reapplyMutations"/> and saves, retrying with a fresh reload of the
    /// affected balances on a concurrency conflict (dev guide §13.4: "concurrent issue operations
    /// cannot corrupt balance"). The mutation delegate must be safe to invoke more than once - it
    /// re-reads each balance's current in-memory state on every attempt.
    /// </summary>
    private async Task SaveWithConcurrencyRetryAsync(IReadOnlyList<StockBalance> balancesToReloadOnConflict, Action reapplyMutations, CancellationToken cancellationToken)
    {
        // Higher than a minimal 3 attempts, with jittered backoff between retries - a burst of
        // many simultaneous writers against the same row (e.g. ten concurrent issues) can collide
        // more than a couple of times in a row; backoff spaces retries out so the collision
        // probability drops quickly instead of every contender racing again immediately.
        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                reapplyMutations();
            }
            catch (InvalidOperationException ex)
            {
                // A domain-level business rule violation (insufficient available stock, over-release,
                // ...) is a client error, not a server fault - map it the same way every other use
                // case's expected failures are mapped, instead of letting it fall through to a 500.
                throw new ConflictException(ex.Message);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (ConcurrencyConflictException) when (attempt < maxAttempts)
            {
                foreach (var balance in balancesToReloadOnConflict)
                {
                    await stockBalances.ReloadAsync(balance, cancellationToken);
                }

                await Task.Delay(Random.Shared.Next(5, 5 * attempt), cancellationToken);
            }
        }
    }

    /// <summary>Fires only on the crossing transition (dev guide §13.4: "one appropriate alert per configured deduplication window") - a stand-in for a time-based window, which neither governing doc specifies.</summary>
    private async Task RaiseLowStockIfCrossedAsync(Guid tenantId, Guid warehouseId, Guid productId, int before, int after, CancellationToken cancellationToken)
    {
        var threshold = await GetEffectiveReorderLevelAsync(tenantId, warehouseId, productId, cancellationToken);
        if (threshold is null || before < threshold.Value || after >= threshold.Value)
        {
            return;
        }

        events.Record("LowStockDetected", tenantId, nameof(Product), productId.ToString(), new { warehouseId, QuantityOnHand = after, ReorderLevel = threshold.Value });
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<int?> GetEffectiveReorderLevelAsync(Guid tenantId, Guid warehouseId, Guid productId, CancellationToken cancellationToken)
    {
        var rule = await reorderRules.FindForAsync(tenantId, warehouseId, productId, cancellationToken);
        if (rule is not null)
        {
            return rule.ReorderLevel;
        }

        var product = await products.GetByIdAsync(tenantId, productId, cancellationToken);
        return product?.ReorderLevel;
    }

    private static StockBalanceResponse ToResponse(StockBalance balance) =>
        new(balance.WarehouseId, balance.ProductId, balance.QuantityOnHand, balance.QuantityReserved, balance.QuantityAvailable);

    private static StockLedgerEntryResponse ToResponse(StockLedgerEntry entry) =>
        new(entry.Id, entry.WarehouseId, entry.ProductId, entry.EventType.ToString(), entry.Quantity, entry.ActorId, entry.OccurredAt, entry.ReferenceType, entry.ReferenceId, entry.Reason);

    private static GoodsReceiptResponse ToResponse(GoodsReceipt receipt) =>
        new(receipt.Id, receipt.WarehouseId, receipt.ReceivedByUserId, receipt.ReceivedAt, receipt.PurchaseOrderId, receipt.Reference,
            receipt.Lines.Select(l => new GoodsReceiptLineResponse(l.ProductId, l.Quantity)).ToList());

    private static StockAdjustmentResponse ToResponse(StockAdjustment adjustment) =>
        new(adjustment.Id, adjustment.WarehouseId, adjustment.ProductId, adjustment.QuantityDelta, adjustment.Reason, adjustment.ActorId, adjustment.OccurredAt);

    private static StockTransferResponse ToResponse(StockTransfer transfer) =>
        new(transfer.Id, transfer.FromWarehouseId, transfer.ToWarehouseId, transfer.ProductId, transfer.Quantity, transfer.ActorId, transfer.OccurredAt);

    private static StockReservationResponse ToResponse(StockReservation reservation) =>
        new(reservation.Id, reservation.WarehouseId, reservation.ProductId, reservation.Quantity, reservation.Status.ToString(), reservation.CreatedAt, reservation.ReleasedAt);
}
