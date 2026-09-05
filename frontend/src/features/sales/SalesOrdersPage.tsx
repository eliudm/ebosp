import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listBranchNames, listCustomerNames, listWarehouseNames } from '../../services/lookupApi'
import { createSalesOrder, listQuotations, listSalesOrders } from '../../services/salesApi'
import type { QuotationResponse, SalesOrderResponse } from '../../types/sales'
import { formatCurrency } from '../../utils/format'

export function SalesOrdersPage() {
  const [orders, setOrders] = useState<SalesOrderResponse[]>([])
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [branchNames, setBranchNames] = useState<Map<string, string>>(new Map())
  const [warehouseNames, setWarehouseNames] = useState<Map<string, string>>(new Map())
  const [acceptedQuotations, setAcceptedQuotations] = useState<QuotationResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listSalesOrders({ pageSize: 100 })
      .then((result) => setOrders(result.items))
      .finally(() => setIsLoading(false))
  }

  function reloadAcceptedQuotations() {
    listQuotations({ pageSize: 100 }).then((result) => setAcceptedQuotations(result.items.filter((q) => q.status === 'Accepted')))
  }

  useEffect(reload, [])
  useEffect(() => {
    listCustomerNames().then(setCustomerNames)
    listBranchNames().then(setBranchNames)
    listWarehouseNames().then(setWarehouseNames)
    reloadAcceptedQuotations()
  }, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Sales orders</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New order
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Customer', render: (row: SalesOrderResponse) => customerNames.get(row.customerId) ?? row.customerId },
          { header: 'Branch', render: (row: SalesOrderResponse) => branchNames.get(row.branchId) ?? row.branchId },
          { header: 'Warehouse', render: (row: SalesOrderResponse) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'Total', render: (row: SalesOrderResponse) => formatCurrency(row.total), align: 'right' },
          { header: 'Status', render: (row: SalesOrderResponse) => row.status },
        ]}
        rows={orders}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No sales orders yet - accept a quotation first."
      />
      {isModalOpen && (
        <CreateSalesOrderModal
          acceptedQuotations={acceptedQuotations}
          warehouseNames={warehouseNames}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
            reloadAcceptedQuotations()
          }}
        />
      )}
    </div>
  )
}

function CreateSalesOrderModal({
  acceptedQuotations,
  warehouseNames,
  onClose,
  onCreated,
}: {
  acceptedQuotations: QuotationResponse[]
  warehouseNames: Map<string, string>
  onClose: () => void
  onCreated: () => void
}) {
  const [quotationId, setQuotationId] = useState('')
  const [warehouseId, setWarehouseId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const warehouseOptions = Array.from(warehouseNames.entries())

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createSalesOrder({ quotationId, warehouseId })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the sales order.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New sales order" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Accepted quotation</label>
          <select required value={quotationId} onChange={(event) => setQuotationId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {acceptedQuotations.length === 0 ? 'No accepted quotations yet' : 'Select a quotation'}
            </option>
            {acceptedQuotations.map((quotation) => (
              <option key={quotation.id} value={quotation.id}>
                {quotation.id.slice(0, 8)} — total {quotation.total}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Warehouse</label>
          <select required value={warehouseId} onChange={(event) => setWarehouseId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {warehouseOptions.length === 0 ? 'No warehouses yet' : 'Select a warehouse'}
            </option>
            {warehouseOptions.map(([id, name]) => (
              <option key={id} value={id}>
                {name}
              </option>
            ))}
          </select>
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create order'}
        </button>
      </form>
    </Modal>
  )
}
