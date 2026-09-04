namespace EBOSP.Contracts.Reporting;

public sealed record InventoryReportResponse(
    decimal TotalValuation,
    DateTimeOffset MovementFrom,
    DateTimeOffset MovementTo,
    int TotalReceived,
    int TotalIssued,
    int TotalAdjustedNet,
    int TotalTransferred);
