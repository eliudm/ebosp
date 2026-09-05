import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { DocumentsModal } from '../../components/DocumentsModal'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listCustomerNames } from '../../services/lookupApi'
import { createInvoice, listInvoices, listSalesOrders } from '../../services/salesApi'
import type { InvoiceResponse, SalesOrderResponse } from '../../types/sales'
import { formatCurrency } from '../../utils/format'

export function InvoicesPage() {
  const [invoices, setInvoices] = useState<InvoiceResponse[]>([])
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [fulfilledOrders, setFulfilledOrders] = useState<SalesOrderResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [documentsFor, setDocumentsFor] = useState<InvoiceResponse | null>(null)

  function reload() {
    setIsLoading(true)
    listInvoices({ pageSize: 100 })
      .then((result) => setInvoices(result.items))
      .finally(() => setIsLoading(false))
  }

  function reloadFulfilledOrders() {
    listSalesOrders({ pageSize: 100 }).then((result) => setFulfilledOrders(result.items.filter((order) => order.status === 'Fulfilled')))
  }

  useEffect(reload, [])
  useEffect(() => {
    listCustomerNames().then(setCustomerNames)
    reloadFulfilledOrders()
  }, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Invoices</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New invoice
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Customer', render: (row: InvoiceResponse) => customerNames.get(row.customerId) ?? row.customerId },
          { header: 'Total', render: (row: InvoiceResponse) => formatCurrency(row.total), align: 'right' },
          { header: 'Status', render: (row: InvoiceResponse) => row.status },
          {
            header: 'Documents',
            render: (row: InvoiceResponse) => (
              <button type="button" onClick={() => setDocumentsFor(row)} className="text-sm text-violet-600 hover:underline">
                Documents
              </button>
            ),
            align: 'right',
          },
        ]}
        rows={invoices}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No invoices yet - deliver a sales order first."
      />
      {documentsFor && (
        <DocumentsModal entityType="Invoice" entityId={documentsFor.id} title={`invoice ${documentsFor.id.slice(0, 8)}`} onClose={() => setDocumentsFor(null)} />
      )}
      {isModalOpen && (
        <CreateInvoiceModal
          fulfilledOrders={fulfilledOrders}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
            reloadFulfilledOrders()
          }}
        />
      )}
    </div>
  )
}

function CreateInvoiceModal({ fulfilledOrders, onClose, onCreated }: { fulfilledOrders: SalesOrderResponse[]; onClose: () => void; onCreated: () => void }) {
  const [salesOrderId, setSalesOrderId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createInvoice({ salesOrderId })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the invoice.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New invoice" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Fulfilled sales order</label>
          <select required value={salesOrderId} onChange={(event) => setSalesOrderId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {fulfilledOrders.length === 0 ? 'No fulfilled sales orders' : 'Select an order'}
            </option>
            {fulfilledOrders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.id.slice(0, 8)} — total {order.total}
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
          {isSubmitting ? 'Creating…' : 'Create invoice'}
        </button>
      </form>
    </Modal>
  )
}
