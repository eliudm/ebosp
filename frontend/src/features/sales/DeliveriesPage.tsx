import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listWarehouseNames } from '../../services/lookupApi'
import { createDelivery, listDeliveries, listSalesOrders } from '../../services/salesApi'
import type { DeliveryResponse, SalesOrderResponse } from '../../types/sales'
import { formatDate } from '../../utils/format'

export function DeliveriesPage() {
  const [deliveries, setDeliveries] = useState<DeliveryResponse[]>([])
  const [warehouseNames, setWarehouseNames] = useState<Map<string, string>>(new Map())
  const [openOrders, setOpenOrders] = useState<SalesOrderResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listDeliveries({ pageSize: 100 })
      .then((result) => setDeliveries(result.items))
      .finally(() => setIsLoading(false))
  }

  function reloadOpenOrders() {
    listSalesOrders({ pageSize: 100 }).then((result) => setOpenOrders(result.items.filter((order) => order.status === 'Open')))
  }

  useEffect(reload, [])
  useEffect(() => {
    listWarehouseNames().then(setWarehouseNames)
    reloadOpenOrders()
  }, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Deliveries</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New delivery
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Sales order', render: (row: DeliveryResponse) => row.salesOrderId.slice(0, 8) },
          { header: 'Warehouse', render: (row: DeliveryResponse) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'Delivered', render: (row: DeliveryResponse) => formatDate(row.deliveredAt) },
        ]}
        rows={deliveries}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No deliveries yet - create a sales order first."
      />
      {isModalOpen && (
        <CreateDeliveryModal
          openOrders={openOrders}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
            reloadOpenOrders()
          }}
        />
      )}
    </div>
  )
}

function CreateDeliveryModal({ openOrders, onClose, onCreated }: { openOrders: SalesOrderResponse[]; onClose: () => void; onCreated: () => void }) {
  const [salesOrderId, setSalesOrderId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createDelivery({ salesOrderId })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the delivery.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New delivery" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Open sales order</label>
          <select required value={salesOrderId} onChange={(event) => setSalesOrderId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm">
            <option value="" disabled>
              {openOrders.length === 0 ? 'No open sales orders' : 'Select an order'}
            </option>
            {openOrders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.id.slice(0, 8)} — total {order.total}
              </option>
            ))}
          </select>
        </div>
        <p className="text-xs text-gray-500">Delivers the entire order - partial shipments aren't supported yet.</p>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Delivering…' : 'Deliver'}
        </button>
      </form>
    </Modal>
  )
}
