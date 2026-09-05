import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { createProduct, listProducts } from '../../services/masterDataApi'
import type { ProductResponse } from '../../types/masterData'
import { formatCurrency } from '../../utils/format'

export function ProductsPage() {
  const [products, setProducts] = useState<ProductResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listProducts({ pageSize: 100 })
      .then((result) => setProducts(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Products</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New product
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'SKU', render: (row: ProductResponse) => row.sku },
          { header: 'Name', render: (row: ProductResponse) => row.name },
          { header: 'Unit price', render: (row: ProductResponse) => formatCurrency(row.unitPrice), align: 'right' },
          { header: 'Reorder level', render: (row: ProductResponse) => row.reorderLevel, align: 'right' },
          { header: 'Status', render: (row: ProductResponse) => row.status },
        ]}
        rows={products}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No products yet."
      />
      {isModalOpen && (
        <CreateProductModal
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
          }}
        />
      )}
    </div>
  )
}

function CreateProductModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [sku, setSku] = useState('')
  const [name, setName] = useState('')
  const [unitPrice, setUnitPrice] = useState('0')
  const [taxRatePercent, setTaxRatePercent] = useState('0')
  const [reorderLevel, setReorderLevel] = useState('0')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createProduct({
        sku,
        name,
        unitPrice: Number(unitPrice),
        taxRatePercent: Number(taxRatePercent),
        reorderLevel: Number(reorderLevel),
      })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the product.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New product" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label htmlFor="sku" className="mb-1 block text-xs font-medium text-gray-500">
            SKU
          </label>
          <input
            id="sku"
            required
            value={sku}
            onChange={(event) => setSku(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <div>
          <label htmlFor="productName" className="mb-1 block text-xs font-medium text-gray-500">
            Name
          </label>
          <input
            id="productName"
            required
            value={name}
            onChange={(event) => setName(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <div className="grid grid-cols-3 gap-2">
          <div>
            <label htmlFor="unitPrice" className="mb-1 block text-xs font-medium text-gray-500">
              Unit price
            </label>
            <input
              id="unitPrice"
              type="number"
              min={0}
              step="0.01"
              value={unitPrice}
              onChange={(event) => setUnitPrice(event.target.value)}
              className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label htmlFor="taxRatePercent" className="mb-1 block text-xs font-medium text-gray-500">
              Tax %
            </label>
            <input
              id="taxRatePercent"
              type="number"
              min={0}
              max={100}
              step="0.01"
              value={taxRatePercent}
              onChange={(event) => setTaxRatePercent(event.target.value)}
              className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>
          <div>
            <label htmlFor="reorderLevel" className="mb-1 block text-xs font-medium text-gray-500">
              Reorder level
            </label>
            <input
              id="reorderLevel"
              type="number"
              min={0}
              value={reorderLevel}
              onChange={(event) => setReorderLevel(event.target.value)}
              className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create'}
        </button>
      </form>
    </Modal>
  )
}
