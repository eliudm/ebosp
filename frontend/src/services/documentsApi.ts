// Mirrors src/EBOSP.Api/Controllers/DocumentsController.cs.

import { apiDownloadBlob, apiRequest, apiUpload } from './apiClient'
import type { PagedRequest, PagedResult } from '../types/common'
import type { DocumentEntityType, DocumentResponse } from '../types/documents'

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

export function listDocuments(entityType: DocumentEntityType, entityId: string, request: PagedRequest = {}): Promise<PagedResult<DocumentResponse>> {
  return apiRequest<PagedResult<DocumentResponse>>(`/api/v1/documents${query({ entityType, entityId, ...request })}`)
}

export function uploadDocument(entityType: DocumentEntityType, entityId: string, file: File): Promise<DocumentResponse> {
  const formData = new FormData()
  formData.set('entityType', entityType)
  formData.set('entityId', entityId)
  formData.set('file', file)
  return apiUpload<DocumentResponse>('/api/v1/documents', formData)
}

/** Fetches the file's bytes and triggers a browser save using the file name already known from its DocumentResponse. */
export async function downloadDocument(id: string, fileName: string): Promise<void> {
  const blob = await apiDownloadBlob(`/api/v1/documents/${id}/content`)
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
