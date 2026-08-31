namespace EBOSP.Contracts.Identity;

public sealed record UserResponse(Guid Id, string Email, string Status, Guid? PrimaryBranchId);
