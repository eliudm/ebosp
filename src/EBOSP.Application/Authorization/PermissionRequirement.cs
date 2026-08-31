using Microsoft.AspNetCore.Authorization;

namespace EBOSP.Application.Authorization;

public sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
