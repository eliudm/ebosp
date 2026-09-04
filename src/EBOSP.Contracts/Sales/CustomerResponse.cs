namespace EBOSP.Contracts.Sales;

public sealed record CustomerResponse(Guid Id, string Name, decimal CreditLimit, string Status);
