import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { DocumentsModal } from '../../components/DocumentsModal'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listProductNames, listSupplierNames } from '../../services/lookupApi'
import { createPurchaseOrder, listPurchaseOrders, listPurchaseRequests } from '../../services/procurementApi'
import type { PurchaseOrderResponse, PurchaseRequestResponse } from '../../types/procurement'
import { formatCurrency } from '../../utils/format'
import { LineItemsEditor } from '../../components/LineItemsEditor'
import type { LineItem } from '../../components/LineItemsEditor'

export function PurchaseOrdersPage() {
  const [orders, setOrders] = useState<PurchaseOrderResponse[]>([])
  const [supplierNames, setSupplierNames] = useState<Map<string, string>>(new Map())
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [approvedRequests, setApprovedRequests] = useState<PurchaseRequestResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [documentsFor, setDocumentsFor] = useState<PurchaseOrderResponse | null>(null)

  function reload() {
    setIsLoading(true)
    listPurchaseOrders({ pageSize: 100 })
      .then((result) => setOrders(result.items))
      .finally(() => setIsLoading(false))
  }

  function reloadApprovedRequests() {
    listPurchaseRequests({ pageSize: 100 }).then((result) => setApprovedRequests(result.items.filter((request) => request.status === 'Approved')))
  }

  useEffect(reload, [])
  useEffect(() => {
    listSupplierNames().then(setSupplierNames)
    listProductNames().then(setProductNames)
    reloadApprovedRequests()
  }, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Purchase orders</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New purchase order
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'PO number', render: (row: PurchaseOrderResponse) => row.poNumber },
          { header: 'Supplier', render: (row: PurchaseOrderResponse) => supplierNames.get(row.supplierId) ?? row.supplierId },
          { header: 'Status', render: (row: PurchaseOrderResponse) => row.status },
          { header: 'Total', render: (row: PurchaseOrderResponse) => formatCurrency(row.total), align: 'right' },
          {
            header: 'Documents',
            render: (row: PurchaseOrderResponse) => (
              <button type="button" onClick={() => setDocumentsFor(row)} className="text-sm text-violet-600 hover:underline">
                Documents
              </button>
            ),
            align: 'right',
          },
        ]}
        rows={orders}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No purchase orders yet - approve a purchase request first."
      />
      {documentsFor && (
        <DocumentsModal entityType="PurchaseOrder" entityId={documentsFor.id} title={documentsFor.poNumber} onClose={() => setDocumentsFor(null)} />
      )}
      {isModalOpen && (
        <CreatePurchaseOrderModal
          approvedRequests={approvedRequests}
          supplierNames={supplierNames}
          productNames={productNames}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
            reloadApprovedRequests()
          }}
        />
      )}
    </div>
  )
}

function CreatePurchaseOrderModal({
  approvedRequests,
  supplierNames,
  productNames,
  onClose,
  onCreated,
}: {
  approvedRequests: PurchaseRequestResponse[]
  supplierNames: Map<string, string>
  productNames: Map<string, string>
  onClose: () => void
  onCreated: () => void
}) {
  const [purchaseRequestId, setPurchaseRequestId] = useState('')
  const [supplierId, setSupplierId] = useState('')
  const [poNumber, setPoNumber] = useState('')
  const [lines, setLines] = useState<LineItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const supplierOptions = Array.from(supplierNames.entries())

  function handleSelectRequest(id: string) {
    setPurchaseRequestId(id)
    const request = approvedRequests.find((r) => r.id === id)
    setLines(request ? request.lines.map((line) => ({ productId: line.productId, quantity: line.quantity, unitPrice: line.estimatedUnitPrice })) : [])
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createPurchaseOrder({
        purchaseRequestId,
        supplierId,
        poNumber,
        lines: lines.map((line) => ({ productId: line.productId, quantity: line.quantity, unitPrice: line.unitPrice })),
      })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the purchase order.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New purchase order" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Approved purchase request</label>
          <select
            required
            value={purchaseRequestId}
            onChange={(event) => handleSelectRequest(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          >
            <option value="" disabled>
              {approvedRequests.length === 0 ? 'No approved requests yet' : 'Select a request'}
            </option>
            {approvedRequests.map((request) => (
              <option key={request.id} value={request.id}>
                {request.justification}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Supplier</label>
          <select required value={supplierId} onChange={(event) => setSupplierId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {supplierOptions.length === 0 ? 'No suppliers yet' : 'Select a supplier'}
            </option>
            {supplierOptions.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">PO number</label>
          <input required value={poNumber} onChange={(event) => setPoNumber(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
        </div>
        {lines.length > 0 && <LineItemsEditor productNames={productNames} lines={lines} onChange={setLines} />}
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting || lines.length === 0}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create purchase order'}
        </button>
      </form>
    </Modal>
  )
}
