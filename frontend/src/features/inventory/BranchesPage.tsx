import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { DataTable } from '../../components/DataTable'
import { Modal } from '../../components/Modal'
import { ApiError } from '../../services/apiClient'
import { createBranch, listBranches } from '../../services/masterDataApi'
import type { BranchResponse } from '../../types/masterData'

export function BranchesPage() {
  const [branches, setBranches] = useState<BranchResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)

  function reload() {
    setIsLoading(true)
    listBranches({ pageSize: 100 })
      .then((result) => setBranches(result.items))
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [])

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Branches</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New branch
        </button>
      </div>
      <DataTable
        columns={[
          { header: 'Name', render: (row: BranchResponse) => row.name },
          { header: 'Status', render: (row: BranchResponse) => row.status },
        ]}
        rows={branches}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No branches yet - create one to get started."
      />
      {isModalOpen && (
        <CreateBranchModal
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

function CreateBranchModal({ onClose, onCreated }: { onClose: () => void; onCreated: () => void }) {
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createBranch({ name })
      onCreated()
    } catch (err) {
      setError(err instanceof ApiError && err.problem?.detail ? err.problem.detail : 'Could not create the branch.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New branch" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <label htmlFor="branchName" className="mb-1 block text-xs font-medium text-gray-500">
          Name
        </label>
        <input
          id="branchName"
          required
          minLength={1}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="mb-3 w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
        />
        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}
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
