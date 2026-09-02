// Mirrors src/EBOSP.Contracts/Identity/*.cs - kept in sync by hand until the backend OpenAPI
// contract is generated into this file (frontend/src/types/README.md).

export interface LoginRequest {
  email: string
  password: string
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface CreateTenantRequest {
  tenantName: string
  adminEmail: string
  adminPassword: string
}

export interface TenantResponse {
  tenantId: string
  tenantName: string
  adminUserId: string
  adminEmail: string
}

export interface PasswordResetRequest {
  email: string
}

export interface PasswordResetConfirmRequest {
  token: string
  newPassword: string
}
