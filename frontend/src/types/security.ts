// Mirrors src/EBOSP.Contracts/{Audit/AuditEventResponse,Security/SecurityAlertResponse,Security/ResolveSecurityAlertRequest}.cs.

export interface AuditEventResponse {
  id: string
  eventType: string
  actorId: string | null
  aggregateType: string
  aggregateId: string
  occurredAt: string
  severity: 'Low' | 'Medium' | 'High' | 'Critical'
  correlationId: string | null
  payload: string
}

export interface SecurityAlertResponse {
  id: string
  rule: string
  severity: 'Low' | 'Medium' | 'High' | 'Critical'
  description: string
  relatedActorId: string | null
  relatedAggregateType: string | null
  relatedAggregateId: string | null
  status: 'Open' | 'Acknowledged' | 'Investigating' | 'Resolved' | 'FalsePositive'
  createdAt: string
  acknowledgedByUserId: string | null
  acknowledgedAt: string | null
  resolvedByUserId: string | null
  resolvedAt: string | null
  resolutionNotes: string | null
}

export interface ResolveSecurityAlertRequest {
  notes: string
}
