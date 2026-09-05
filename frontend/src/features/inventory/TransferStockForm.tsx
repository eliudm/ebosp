import { useState } from 'react'
import type { FormEvent } from 'react'
import { Modal } from '../../components/Modal'
import { ApiError } from '../../services/apiClient'
import { transferStock } from '../../services/inventoryApi'
import { ProductSelect, QuantityInput, WarehouseSelect } from './ReceiveStockForm'

interface Props {
  warehouseNames: Map<string, string>
  productNames: Map<string, string>
  onClose: () => void
  onSuccess: () => void
}

export function TransferStockForm({ warehouseNames, productNames, onClose, onSuccess }: Props) {
  const [fromWarehouseId, setFromWarehouseId] = useState('')
  const [toWarehouseId, setToWarehouseId] = useState('')
  const [productId, setProductId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await transferStock({
        fromWarehouseId,
        toWarehouseId,
        productId,
        quantity: Number(quantity),
        reason: reason || undefined,
        idempotencyKey: crypto.randomUUID(),
      })
      onSuccess()
    } catch (err) {
      setError(err instanceof ApiError && err.problem?.detail ? err.problem.detail : 'Could not transfer stock.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="Transfer stock" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <WarehouseSelect value={fromWarehouseId} onChange={setFromWarehouseId} names={warehouseNames} label="From warehouse" />
        <WarehouseSelect value={toWarehouseId} onChange={setToWarehouseId} names={warehouseNames} label="To warehouse" />
        <ProductSelect value={productId} onChange={setProductId} names={productNames} />
        <QuantityInput value={quantity} onChange={setQuantity} />
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Reason (optional)</label>
          <input value={reason} onChange={(event) => setReason(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Transferring…' : 'Transfer'}
        </button>
      </form>
    </Modal>
  )
}
