import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { createSupplier, listSuppliers } from '../../services/procurementApi'
import type { SupplierResponse } from '../../types/procurement'

export function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<SupplierResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listSuppliers({ pageSize: 100 })
      .then((result) => setSuppliers(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Suppliers</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New supplier
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Name', render: (row: SupplierResponse) => row.name },
          { header: 'Tax ID', render: (row: SupplierResponse) => row.taxId ?? '—' },
          { header: 'Contact', render: (row: SupplierResponse) => row.contact ?? '—' },
          { header: 'Status', render: (row: SupplierResponse) => row.status },
        ]}
        rows={suppliers}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No suppliers yet."
      />
      {isModalOpen && (
        <CreateSupplierModal
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

function CreateSupplierModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState('')
  const [taxId, setTaxId] = useState('')
  const [contact, setContact] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createSupplier({ name, taxId: taxId || undefined, contact: contact || undefined })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the supplier.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New supplier" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Name</label>
          <input required value={name} onChange={(event) => setName(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Tax ID (optional)</label>
          <input value={taxId} onChange={(event) => setTaxId(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Contact (optional)</label>
          <input value={contact} onChange={(event) => setContact(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
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
