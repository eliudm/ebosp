import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { listBranchNames } from '../../services/lookupApi'
import { createWarehouse, listWarehouses } from '../../services/masterDataApi'
import type { WarehouseResponse } from '../../types/masterData'

export function WarehousesPage() {
  const [warehouses, setWarehouses] = useState<WarehouseResponse[]>([])
  const [branchNames, setBranchNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listWarehouses({ pageSize: 100 })
      .then((result) => setWarehouses(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])
  useEffect(() => {
    listBranchNames().then(setBranchNames)
  }, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Warehouses</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New warehouse
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Name', render: (row: WarehouseResponse) => row.name },
          { header: 'Branch', render: (row: WarehouseResponse) => branchNames.get(row.branchId) ?? row.branchId },
          { header: 'Status', render: (row: WarehouseResponse) => row.status },
        ]}
        rows={warehouses}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No warehouses yet - create a branch first, then a warehouse."
      />
      {isModalOpen && (
        <CreateWarehouseModal
          branchNames={branchNames}
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

function CreateWarehouseModal({
  branchNames,
  onClose,
  onCreated,
}: {
  branchNames: Map<string, string>
  onClose: () => void
  onCreated: () => void
}) {
  const [name, setName] = useState('')
  const [branchId, setBranchId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const branchOptions = Array.from(branchNames.entries())

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createWarehouse({ name, branchId })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the warehouse.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New warehouse" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <label htmlFor="warehouseName" className="mb-1 block text-xs font-medium text-gray-500">
          Name
        </label>
        <input
          id="warehouseName"
          required
          minLength={1}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="mb-3 w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
        />
        <label htmlFor="warehouseBranch" className="mb-1 block text-xs font-medium text-gray-500">
          Branch
        </label>
        <select
          id="warehouseBranch"
          required
          value={branchId}
          onChange={(event) => setBranchId(event.target.value)}
          className="mb-3 w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
        >
          <option value="" disabled>
            {branchOptions.length === 0 ? 'No branches yet' : 'Select a branch'}
          </option>
          {branchOptions.map(([id, branchName]) => (
            <option key={id} value={id}>
              {branchName}
            </option>
          ))}
        </select>
        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting || branchOptions.length === 0}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create'}
        </button>
      </form>
    </Modal>
  )
}
