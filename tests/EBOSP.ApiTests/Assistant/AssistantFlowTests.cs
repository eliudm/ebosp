using System.Net;
using System.Net.Http.Json;
using EBOSP.ApiTests.Procurement;
using EBOSP.Application.Assistant;
using EBOSP.Contracts.Assistant;
using EBOSP.Contracts.Inventory;

namespace EBOSP.ApiTests.Assistant;

/// <summary>
/// Spec §26/dev guide §43's read-only AI assistant. The LLM itself is never called in this suite -
/// <see cref="FakeAiCompletionClient"/> stands in (swapped via <see cref="CustomWebApplicationFactory"/>,
/// same DI pattern as <see cref="CapturingPasswordResetNotifier"/>) - so these tests prove the
/// orchestration (tool selection, per-tool authorization, tenant scoping, the bounded retry loop),
/// not whether a real model would phrase a good answer.
/// </summary>
public class AssistantFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task LowStockQuestion_ExecutesLowStockToolAgainstRealData()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (_, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        // Product's default ReorderLevel from CreateBranchWarehouseAndProductAsync is 0 (no threshold) -
        // give it a real one directly so a small receipt still counts as "below reorder level".
        var lowProductResponse = await adminClient.PostAsJsonAsync("/api/v1/products", new EBOSP.Contracts.MasterData.CreateProductRequest
        {
            Sku = $"SKU-{Guid.NewGuid():N}",
            Name = "Low Stock Widget",
            UnitPrice = 10m,
            TaxRatePercent = 16m,
            ReorderLevel = 10,
        });
        var lowProduct = (await lowProductResponse.Content.ReadFromJsonAsync<EBOSP.Contracts.MasterData.ProductResponse>())!;
        (await adminClient.PostAsJsonAsync("/api/v1/inventory/receipts", new ReceiveStockRequest
        {
            WarehouseId = warehouseId,
            Lines = [new ReceiveStockLine { ProductId = lowProduct.Id, Quantity = 2 }],
        })).EnsureSuccessStatusCode();

        const string question = "Which products are below reorder level?";
        factory.AiCompletionClient.Script(
            question,
            new AiCompletionTurn(null, [new AiToolCall("call-1", "low_stock", "{}")]),
            new AiCompletionTurn("A few products need restocking.", []));

        var response = await adminClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = question });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<AskAssistantResponse>())!;
        Assert.Equal("A few products need restocking.", result.Answer);
        Assert.Equal(["low_stock"], result.ToolsUsed);

        var history = factory.AiCompletionClient.CallHistoryFor(question);
        var toolResultMessage = history[1].Single(m => m.Role == AiMessageRole.ToolResult);
        Assert.Contains(lowProduct.Id.ToString(), toolResultMessage.ToolResults!.Single().ResultJson);
    }

    [Fact]
    public async Task AmbiguousQuestion_NoToolCall_ReturnsPlainTextAnswer()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        // No Script() call for this question - FakeAiCompletionClient's default is a plain-text,
        // no-tool-call turn, exactly modeling an ambiguous/unanswerable question.
        var response = await adminClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = "What's the meaning of life?" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<AskAssistantResponse>())!;
        Assert.Equal("I don't have information about that.", result.Answer);
        Assert.Empty(result.ToolsUsed);
    }

    [Fact]
    public async Task UnusualStockAdjustmentsQuestion_UserWithAuditRead_ToolIsOfferedAndExecuted()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (_, warehouseId, productId) = await ProcurementTestHelpers.CreateBranchWarehouseAndProductAsync(adminClient);
        // Default LargeAdjustmentThreshold is 100 - well above it, so this raises StockAdjustmentFlagged.
        (await adminClient.PostAsJsonAsync("/api/v1/inventory/adjustments", new AdjustStockRequest
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            QuantityDelta = 150,
            Reason = "Cycle count correction",
        })).EnsureSuccessStatusCode();

        const string question = "Show unusual stock adjustments.";
        factory.AiCompletionClient.Script(
            question,
            new AiCompletionTurn(null, [new AiToolCall("call-1", "unusual_stock_adjustments", "{}")]),
            new AiCompletionTurn("One large adjustment was flagged recently.", []));

        var response = await adminClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = question });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<AskAssistantResponse>())!;
        Assert.Equal(["unusual_stock_adjustments"], result.ToolsUsed);
        Assert.Contains("unusual_stock_adjustments", factory.AiCompletionClient.OfferedToolsFor(question)!);

        var history = factory.AiCompletionClient.CallHistoryFor(question);
        var toolResultMessage = history[1].Single(m => m.Role == AiMessageRole.ToolResult);
        Assert.Contains("StockAdjustmentFlagged", toolResultMessage.ToolResults!.Single().ResultJson);
    }

    [Fact]
    public async Task UnusualStockAdjustmentsQuestion_UserWithoutAuditRead_ToolNeverOfferedNorExecuted()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);
        var (salesClient, _) = await ProcurementTestHelpers.CreateUserWithRoleAsync(adminClient, factory, "sales-officer");

        const string question = "Show unusual stock adjustments (unauthorized attempt).";
        // Even if a compromised/buggy model tried to call the tool anyway, the server must refuse -
        // script exactly that attempt rather than a well-behaved response.
        factory.AiCompletionClient.Script(
            question,
            new AiCompletionTurn(null, [new AiToolCall("call-1", "unusual_stock_adjustments", "{}")]),
            new AiCompletionTurn("I can't retrieve that.", []));

        var response = await salesClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = question });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The tool must never even be offered to the model for a user without audit.read.
        Assert.DoesNotContain("unusual_stock_adjustments", factory.AiCompletionClient.OfferedToolsFor(question)!);

        var history = factory.AiCompletionClient.CallHistoryFor(question);
        var toolResultMessage = history[1].Single(m => m.Role == AiMessageRole.ToolResult);
        var toolResult = toolResultMessage.ToolResults!.Single();
        // The server-side re-check (ExecuteToolAsync's catalogue guard) refused to run the tool,
        // even though the fake model called it anyway.
        Assert.True(toolResult.IsError);
        Assert.DoesNotContain("StockAdjustmentFlagged", toolResult.ResultJson);
    }

    [Fact]
    public async Task ToolCallLoop_ExceedsCap_ReturnsFallbackAnswerInsteadOfLooping()
    {
        var adminClient = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(adminClient);
        AuthTestHelpers.AuthorizeAs(adminClient, admin.Tokens);

        const string question = "Keep asking for tools forever.";
        // Scripted to always request another tool call, never a final text turn - the service's
        // MaxToolCallsPerRequest bound (default 3) must still terminate the request.
        factory.AiCompletionClient.Script(
            question,
            new AiCompletionTurn(null, [new AiToolCall("call-1", "low_stock", "{}")]),
            new AiCompletionTurn(null, [new AiToolCall("call-2", "low_stock", "{}")]),
            new AiCompletionTurn(null, [new AiToolCall("call-3", "low_stock", "{}")]));

        var response = await adminClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = question });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<AskAssistantResponse>())!;
        Assert.Equal("I wasn't able to complete that request - please try rephrasing or asking a more specific question.", result.Answer);
    }

    [Fact]
    public async Task Ask_WithoutAuthentication_Returns401()
    {
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = "Which products are below reorder level?" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

/// <summary>Runs against its own isolated factory instance with the real (Ai:ApiKey-unset) IAiCompletionClient resolution left in place, so it can't collide with the rest of the suite's faked client - see <see cref="AiUnavailableWebApplicationFactory"/>.</summary>
public class AssistantUnavailableTests(AiUnavailableWebApplicationFactory factory) : IClassFixture<AiUnavailableWebApplicationFactory>
{
    [Fact]
    public async Task Ask_ApiKeyNotConfigured_Returns503()
    {
        var client = factory.CreateClient();
        var admin = await AuthTestHelpers.CreateTenantAdminAsync(client);
        AuthTestHelpers.AuthorizeAs(client, admin.Tokens);

        var response = await client.PostAsJsonAsync("/api/v1/assistant/ask", new AskAssistantRequest { Question = "Which products are below reorder level?" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
