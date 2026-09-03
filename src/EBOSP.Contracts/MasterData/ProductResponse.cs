namespace EBOSP.Contracts.MasterData;

public sealed record ProductResponse(
    Guid Id,
    Guid? CategoryId,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    decimal TaxRatePercent,
    int ReorderLevel,
    string Status);
