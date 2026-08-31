namespace EBOSP.Contracts.Identity;

public sealed record TenantResponse(Guid TenantId, string TenantName, Guid AdminUserId, string AdminEmail);
