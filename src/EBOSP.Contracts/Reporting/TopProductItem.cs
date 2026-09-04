namespace EBOSP.Contracts.Reporting;

public sealed record TopProductItem(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue);
