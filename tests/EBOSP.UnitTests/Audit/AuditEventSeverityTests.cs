using EBOSP.Application.Audit;

namespace EBOSP.UnitTests.Audit;

public class AuditEventSeverityTests
{
    [Theory]
    [InlineData("LoginFailed", "High")]
    [InlineData("StockAdjusted", "Critical")]
    [InlineData("PaymentReceived", "Critical")]
    [InlineData("UserCreated", "Medium")]
    public void Classify_NamedEventType_ReturnsSpecCriticality(string eventType, string expected)
    {
        Assert.Equal(expected, AuditEventSeverity.Classify(eventType));
    }

    [Fact]
    public void Classify_UnnamedEventType_DefaultsToMedium()
    {
        // Event types this project invented by judgment (e.g. StockTransferred, DeliveryCreated)
        // aren't in spec's own event catalogue - defaulting rather than guessing a severity.
        Assert.Equal("Medium", AuditEventSeverity.Classify("SomeEventTypeNotInSpecsCatalogue"));
    }
}
