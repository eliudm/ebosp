using System.Text.Json;
using EBOSP.Application.Audit;
using EBOSP.Application.Common;
using EBOSP.Application.Identity;
using EBOSP.Application.Reporting;
using EBOSP.Contracts.Assistant;
using EBOSP.Contracts.Common;

namespace EBOSP.Application.Assistant;

/// <summary>
/// Translates an authorized business question into one of four predefined tool calls, then
/// summarizes the result (spec §26, dev guide §43) - never unrestricted SQL, never a bypass of
/// authorization. Three tools need nothing beyond authentication (the same gate their own Reports
/// endpoints already use); the fourth (unusual_stock_adjustments) wraps audit-read data, so it is
/// only ever offered to - or executed for - a caller who actually holds audit.read. There is no
/// single [Authorize(Policy=...)] on the controller that could enforce this, since the four tools
/// don't share one permission, so BuildToolCatalogue and ExecuteToolAsync both check
/// ICurrentUserContext.HasPermission directly (dev guide §43: "AI receives only data the
/// requesting user is already permitted to see").
/// </summary>
public sealed class AssistantService(
    IReportingService reports,
    IAuditEventQueryService auditEvents,
    ICurrentUserContext currentUser,
    IAiCompletionClient client,
    IDomainEventRecorder events,
    IUnitOfWork unitOfWork,
    IClock clock,
    AiOptions options) : IAssistantService
{
    private const string LowStockTool = "low_stock";
    private const string TopProductsTool = "top_products";
    private const string OverdueInvoicesTool = "overdue_invoices";
    private const string UnusualStockAdjustmentsTool = "unusual_stock_adjustments";

    private const string SystemPrompt =
        "You are a read-only business assistant for an ERP system. Answer the user's question " +
        "using only the provided tools - never invent figures. If no tool fits the question, say " +
        "so plainly instead of guessing. Keep answers brief and factual.";

    public async Task<AskAssistantResponse> AskAsync(Guid tenantId, Guid actingUserId, AskAssistantRequest request, CancellationToken cancellationToken)
    {
        var tools = BuildToolCatalogue();
        var messages = new List<AiMessage> { new(AiMessageRole.User, request.Question) };
        var toolsUsed = new List<string>();

        string? answer = null;
        for (var iteration = 0; iteration < options.MaxToolCallsPerRequest; iteration++)
        {
            var turn = await client.CompleteAsync(SystemPrompt, messages, tools, cancellationToken);
            answer = turn.Text;
            if (turn.ToolCalls.Count == 0)
            {
                break;
            }

            messages.Add(new AiMessage(AiMessageRole.Assistant, turn.Text, turn.ToolCalls));

            var results = new List<AiToolResult>();
            foreach (var call in turn.ToolCalls)
            {
                toolsUsed.Add(call.Name);
                results.Add(await ExecuteToolAsync(tenantId, tools, call, cancellationToken));
            }

            messages.Add(new AiMessage(AiMessageRole.ToolResult, ToolResults: results));
        }

        answer ??= "I wasn't able to complete that request - please try rephrasing or asking a more specific question.";
        var distinctToolsUsed = toolsUsed.Distinct().ToList();

        // "Log AI requests and tool usage without storing unnecessary sensitive prompts/data"
        // (dev guide §43) - deliberately omits the question and answer text, neither of which the
        // rule requires and either of which could carry business-sensitive content.
        events.Record(
            "AiAssistantQueried",
            tenantId,
            "AiAssistant",
            actingUserId.ToString(),
            new { ToolsUsed = distinctToolsUsed, ToolCallCount = toolsUsed.Count },
            actorId: actingUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AskAssistantResponse(answer, distinctToolsUsed);
    }

    private List<AiToolDefinition> BuildToolCatalogue()
    {
        var tools = new List<AiToolDefinition>
        {
            new(LowStockTool, "Lists products whose on-hand quantity is below their reorder level.",
                """{"type":"object","properties":{},"additionalProperties":false}"""),
            new(TopProductsTool, "Returns the best-selling products by revenue over a lookback window.",
                """{"type":"object","properties":{"days":{"type":"integer","description":"Lookback window in days, default 30"},"top":{"type":"integer","description":"Number of products to return, default 5, max 20"}},"additionalProperties":false}"""),
            new(OverdueInvoicesTool,
                "Lists invoices that are not yet fully paid. This system has no due-date concept, so " +
                "\"overdue\" here means \"still outstanding\" (issued but not fully paid), not aged past a due date.",
                """{"type":"object","properties":{},"additionalProperties":false}"""),
        };

        if (currentUser.HasPermission(PermissionCodes.AuditRead))
        {
            tools.Add(new AiToolDefinition(
                UnusualStockAdjustmentsTool,
                "Lists recent large/unusual stock adjustments flagged for review.",
                """{"type":"object","properties":{"days":{"type":"integer","description":"Lookback window in days, default 30"}},"additionalProperties":false}"""));
        }

        return tools;
    }

    private async Task<AiToolResult> ExecuteToolAsync(Guid tenantId, IReadOnlyList<AiToolDefinition> catalogue, AiToolCall call, CancellationToken cancellationToken)
    {
        // Defensive re-check against the catalogue actually offered this request - not just trust
        // that the model only ever calls what it was given. A buggy or adversarial model response
        // must not be able to reach a tool (e.g. the audit one) it was never told about.
        if (catalogue.All(t => t.Name != call.Name))
        {
            return new AiToolResult(call.Id, """{"error":"Tool not available for this request."}""", IsError: true);
        }

        try
        {
            var input = string.IsNullOrWhiteSpace(call.InputJson) ? default : JsonDocument.Parse(call.InputJson).RootElement;
            object result = call.Name switch
            {
                LowStockTool => await reports.GetLowStockAsync(tenantId, new PagedRequest { PageSize = 20 }, cancellationToken),
                TopProductsTool => await reports.GetTopProductsAsync(tenantId, clock.UtcNow.AddDays(-ClampDays(input)), clock.UtcNow, ClampTop(input), cancellationToken),
                OverdueInvoicesTool => await reports.GetOutstandingInvoicesAsync(tenantId, new PagedRequest { PageSize = 20 }, cancellationToken),
                UnusualStockAdjustmentsTool => await ExecuteUnusualStockAdjustmentsAsync(tenantId, input, cancellationToken),
                _ => throw new InvalidOperationException($"Unhandled tool '{call.Name}'."),
            };

            return new AiToolResult(call.Id, JsonSerializer.Serialize(result), IsError: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new AiToolResult(call.Id, JsonSerializer.Serialize(new { error = "Tool execution failed: " + ex.Message }), IsError: true);
        }
    }

    private async Task<object> ExecuteUnusualStockAdjustmentsAsync(Guid tenantId, JsonElement input, CancellationToken cancellationToken)
    {
        // Belt-and-suspenders: even if a caller without audit.read somehow reached this branch, the
        // catalogue check above should already have refused it, but this tool touches data that is
        // gated by permission (not just tenant), so it re-checks rather than trusting the caller.
        if (!currentUser.HasPermission(PermissionCodes.AuditRead))
        {
            return new { error = "Not authorized for this data." };
        }

        var from = clock.UtcNow.AddDays(-ClampDays(input));
        var page = await auditEvents.ListAsync(tenantId, actorId: null, eventType: "StockAdjustmentFlagged", aggregateType: null, aggregateId: null, from, clock.UtcNow, new PagedRequest { PageSize = 20 }, cancellationToken);
        return page.Items;
    }

    private static int ClampDays(JsonElement input) => Math.Clamp(TryGetInt(input, "days") ?? 30, 1, 365);

    private static int ClampTop(JsonElement input) => Math.Clamp(TryGetInt(input, "top") ?? 5, 1, 20);

    private static int? TryGetInt(JsonElement input, string property) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(property, out var value) && value.TryGetInt32(out var number) ? number : null;
}
