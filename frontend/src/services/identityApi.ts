import { apiRequest } from './apiClient'
import type {
  CreateTenantRequest,
  LoginRequest,
  PasswordResetConfirmRequest,
  PasswordResetRequest,
  RefreshTokenRequest,
  TenantResponse,
  TokenResponse,
} from '../types/identity'

export function login(request: LoginRequest): Promise<TokenResponse> {
  return apiRequest<TokenResponse>('/api/v1/auth/login', { method: 'POST', body: request, authenticated: false })
}

export function refresh(request: RefreshTokenRequest): Promise<TokenResponse> {
  return apiRequest<TokenResponse>('/api/v1/auth/refresh', { method: 'POST', body: request, authenticated: false })
}

export function logout(request: RefreshTokenRequest): Promise<void> {
  return apiRequest<void>('/api/v1/auth/logout', { method: 'POST', body: request, authenticated: false })
}

export function createTenant(request: CreateTenantRequest): Promise<TenantResponse> {
  return apiRequest<TenantResponse>('/api/v1/tenants', { method: 'POST', body: request, authenticated: false })
}

export function requestPasswordReset(request: PasswordResetRequest): Promise<void> {
  return apiRequest<void>('/api/v1/auth/password-reset/request', { method: 'POST', body: request, authenticated: false })
}

export function confirmPasswordReset(request: PasswordResetConfirmRequest): Promise<void> {
  return apiRequest<void>('/api/v1/auth/password-reset/confirm', { method: 'POST', body: request, authenticated: false })
}
