import { useState } from 'react'
import type { FormEvent } from 'react'
import { Modal } from '../../components/Modal'
import { ApiError, getErrorMessage } from '../../services/apiClient'
import { adjustStock } from '../../services/inventoryApi'
import { ProductSelect, WarehouseSelect } from './ReceiveStockForm'

interface Props {
  warehouseNames: Map<string, string>
  productNames: Map<string, string>
  onClose: () => void
  onSuccess: () => void
}

export function AdjustStockForm({ warehouseNames, productNames, onClose, onSuccess }: Props) {
  const [warehouseId, setWarehouseId] = useState('')
  const [productId, setProductId] = useState('')
  const [quantityDelta, setQuantityDelta] = useState('0')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await adjustStock({ warehouseId, productId, quantityDelta: Number(quantityDelta), reason, idempotencyKey: crypto.randomUUID() })
      onSuccess()
    } catch (err) {
      if (err instanceof ApiError && err.status === 403) {
        // Large adjustments (over a server-configured threshold) need a permission on top of the
        // base one - see InventoryController.Adjust's imperative check, never hardcoded here.
        setError('This adjustment is large enough to need additional permission, which this account does not have.')
      } else {
        setError(getErrorMessage(err, 'Could not adjust stock.'))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="Adjust stock" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <WarehouseSelect value={warehouseId} onChange={setWarehouseId} names={warehouseNames} />
        <ProductSelect value={productId} onChange={setProductId} names={productNames} />
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Quantity delta (positive to increase, negative to decrease)</label>
          <input
            type="number"
            required
            value={quantityDelta}
            onChange={(event) => setQuantityDelta(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Reason</label>
          <input
            required
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Adjusting…' : 'Adjust'}
        </button>
      </form>
    </Modal>
  )
}
