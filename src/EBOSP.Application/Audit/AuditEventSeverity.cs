namespace EBOSP.Application.Audit;

/// <summary>
/// Severity is the one field spec's audit filters need (dev guide §18: "filters by actor, action,
/// resource, tenant, severity and time") that OutboxMessage doesn't store - it's a pure function of
/// EventType, so it's computed here rather than persisted. Populated for the event types spec's own
/// catalogue (§11) names criticality for; every other event type this project has invented by
/// judgment across M2-M7 defaults to Medium rather than guessing.
/// </summary>
public static class AuditEventSeverity
{
    private static readonly Dictionary<string, string> Criticality = new()
    {
        ["UserCreated"] = "Medium",
        ["LoginFailed"] = "High",
        ["PurchaseRequestSubmitted"] = "High",
        ["PurchaseApproved"] = "High",
        ["PurchaseOrderCreated"] = "Medium",
        ["GoodsReceived"] = "High",
        ["StockReceived"] = "High",
        ["StockIssued"] = "High",
        ["StockAdjusted"] = "Critical",
        ["SalesOrderCreated"] = "Medium",
        ["InvoiceCreated"] = "High",
        ["PaymentReceived"] = "Critical",
        ["LowStockDetected"] = "Medium",
        ["SecurityAlertRaised"] = "Critical",
        ["DocumentUploaded"] = "Medium",
    };

    public static string Classify(string eventType) => Criticality.GetValueOrDefault(eventType, "Medium");
}
