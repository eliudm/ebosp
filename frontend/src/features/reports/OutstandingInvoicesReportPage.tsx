import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { listCustomerNames } from '../../services/lookupApi'
import { getOutstandingInvoices } from '../../services/reportsApi'
import type { PagedResult } from '../../types/common'
import type { OutstandingInvoiceItem } from '../../types/reporting'
import { formatCurrency, formatDate } from '../../utils/format'

const PAGE_SIZE = 20

export function OutstandingInvoicesReportPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<OutstandingInvoiceItem> | null>(null)
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    listCustomerNames()
      .then(setCustomerNames)
      .catch(() => {
        // Names are a display nicety, not load-bearing - fall back to showing raw ids.
      })
  }, [])

  useEffect(() => {
    setIsLoading(true)
    setError(null)
    getOutstandingInvoices({ page, pageSize: PAGE_SIZE })
      .then(setResult)
      .catch(() => setError('Could not load the outstanding invoices report.'))
      .finally(() => setIsLoading(false))
  }, [page])

  return (
    <div>
      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}
      <DataTable
        columns={[
          { header: 'Customer', render: (row) => customerNames.get(row.customerId) ?? row.customerId },
          { header: 'Invoiced', render: (row) => formatDate(row.createdAt) },
          { header: 'Total', render: (row) => formatCurrency(row.total), align: 'right' },
          { header: 'Paid', render: (row) => formatCurrency(row.paidTotal), align: 'right' },
          { header: 'Outstanding', render: (row) => formatCurrency(row.outstanding), align: 'right' },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.invoiceId}
        isLoading={isLoading}
        emptyMessage="No outstanding invoices."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
    </div>
  )
}
