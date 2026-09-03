namespace EBOSP.Contracts.Procurement;

public sealed record SupplierResponse(Guid Id, string Name, string? TaxId, string? Contact, string Status);
