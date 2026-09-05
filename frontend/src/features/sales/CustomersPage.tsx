import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { createCustomer, listCustomers } from '../../services/salesApi'
import type { CustomerResponse } from '../../types/sales'
import { formatCurrency } from '../../utils/format'

export function CustomersPage() {
  const [customers, setCustomers] = useState<CustomerResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listCustomers({ pageSize: 100 })
      .then((result) => setCustomers(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Customers</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New customer
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Name', render: (row: CustomerResponse) => row.name },
          { header: 'Credit limit', render: (row: CustomerResponse) => formatCurrency(row.creditLimit), align: 'right' },
          { header: 'Status', render: (row: CustomerResponse) => row.status },
        ]}
        rows={customers}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No customers yet."
      />
      {isModalOpen && (
        <CreateCustomerModal
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

function CreateCustomerModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState('')
  const [creditLimit, setCreditLimit] = useState('0')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createCustomer({ name, creditLimit: Number(creditLimit) })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the customer.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New customer" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Name</label>
          <input required value={name} onChange={(event) => setName(event.target.value)} className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Credit limit</label>
          <input
            type="number"
            min={0}
            step="0.01"
            value={creditLimit}
            onChange={(event) => setCreditLimit(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
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
