// Mirrors src/EBOSP.Api/Controllers/{AuditEvents,SecurityAlerts}Controller.cs.

import { apiRequest } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type { AuditEventResponse, ResolveSecurityAlertRequest, SecurityAlertResponse } from '../types/security'

interface AuditEventFilter extends PagedRequest {
  eventType?: string
  from?: string
  to?: string
}

function query(params: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      search.set(key, String(value))
    }
  }
  const built = search.toString()
  return built ? `?${built}` : ''
}

export function listAuditEvents(filter: AuditEventFilter = {}): Promise<PagedResult<AuditEventResponse>> {
  return apiRequest<PagedResult<AuditEventResponse>>(`/api/v1/audit-events${query({ ...filter })}`)
}

export function listSecurityAlerts(request: PagedRequest = {}): Promise<PagedResult<SecurityAlertResponse>> {
  return apiRequest<PagedResult<SecurityAlertResponse>>(`/api/v1/security/alerts${query({ ...request })}`)
}

export function acknowledgeAlert(id: string): Promise<SecurityAlertResponse> {
  return apiRequest<SecurityAlertResponse>(`/api/v1/security/alerts/${id}/acknowledge`, { method: 'POST' })
}

export function investigateAlert(id: string): Promise<SecurityAlertResponse> {
  return apiRequest<SecurityAlertResponse>(`/api/v1/security/alerts/${id}/investigate`, { method: 'POST' })
}

export function resolveAlert(id: string, request: ResolveSecurityAlertRequest): Promise<SecurityAlertResponse> {
  return apiRequest<SecurityAlertResponse>(`/api/v1/security/alerts/${id}/resolve`, { method: 'POST', body: request })
}

export function markFalsePositiveAlert(id: string, request: ResolveSecurityAlertRequest): Promise<SecurityAlertResponse> {
  return apiRequest<SecurityAlertResponse>(`/api/v1/security/alerts/${id}/false-positive`, { method: 'POST', body: request })
}
