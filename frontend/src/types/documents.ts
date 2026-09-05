// Mirrors src/EBOSP.Contracts/Documents/DocumentResponse.cs.

export type DocumentEntityType = 'PurchaseOrder' | 'Invoice'

export interface DocumentResponse {
  id: string
  entityType: string
  entityId: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedByUserId: string
  uploadedAt: string
}
