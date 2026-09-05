import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from './DataTable'
import { Modal } from './Modal'
import { Pagination } from './Pagination'
import { getErrorMessage } from '../services/apiClient'
import { downloadDocument, listDocuments, uploadDocument } from '../services/documentsApi'
import type { DocumentEntityType, DocumentResponse } from '../types/documents'
import type { PagedResult } from '../types/common'
import { formatDate, formatFileSize } from '../utils/format'

const PAGE_SIZE = 10

interface DocumentsModalProps {
  entityType: DocumentEntityType
  entityId: string
  title: string
  onClose: () => void
}

/** Shared by PurchaseOrdersPage and InvoicesPage - the only two entity types Documents attaches to. */
export function DocumentsModal({ entityType, entityId, title, onClose }: DocumentsModalProps) {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<DocumentResponse> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  function reload() {
    setIsLoading(true)
    setLoadError(null)
    listDocuments(entityType, entityId, { page, pageSize: PAGE_SIZE, sortDescending: true })
      .then(setResult)
      .catch((err) => setLoadError(getErrorMessage(err, 'Could not load documents.')))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [entityType, entityId, page])

  async function handleUpload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const file = fileInputRef.current?.files?.[0]
    if (!file) {
      return
    }
    setUploadError(null)
    setIsUploading(true)
    try {
      await uploadDocument(entityType, entityId, file)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
      setPage(1)
      reload()
    } catch (err) {
      setUploadError(getErrorMessage(err, 'Could not upload this document.'))
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownload(doc: DocumentResponse) {
    setDownloadingId(doc.id)
    try {
      await downloadDocument(doc.id, doc.fileName)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not download this document.'))
    } finally {
      setDownloadingId(null)
    }
  }

  return (
    <Modal title={`Documents — ${title}`} onClose={onClose} size="lg">
      <form onSubmit={handleUpload} className="mb-4 flex items-center gap-2">
        <input ref={fileInputRef} type="file" required className="flex-1 text-sm" />
        <button
          type="submit"
          disabled={isUploading}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isUploading ? 'Uploading…' : 'Upload'}
        </button>
      </form>
      {uploadError && <p className="mb-3 text-sm text-red-600">{uploadError}</p>}
      {loadError && <p className="mb-3 text-sm text-red-600">{loadError}</p>}
      <DataTable
        columns={[
          { header: 'File name', render: (row: DocumentResponse) => row.fileName },
          { header: 'Size', render: (row: DocumentResponse) => formatFileSize(row.sizeBytes), align: 'right' },
          { header: 'Uploaded', render: (row: DocumentResponse) => formatDate(row.uploadedAt) },
          {
            header: 'Download',
            render: (row: DocumentResponse) => (
              <button
                type="button"
                onClick={() => handleDownload(row)}
                disabled={downloadingId === row.id}
                className="text-sm text-violet-600 hover:underline disabled:opacity-60"
              >
                {downloadingId === row.id ? 'Downloading…' : 'Download'}
              </button>
            ),
            align: 'right',
          },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No documents uploaded yet."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </Modal>
  )
}
