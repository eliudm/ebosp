import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { listCustomerNames } from '../../services/lookupApi'
import { getOutstandingInvoices } from '../../services/financeApi'
import type { PagedResult } from '../../types/common'
import type { OutstandingInvoiceItem } from '../../types/reporting'
import { formatCurrency, formatDate } from '../../utils/format'
import { CreatePaymentModal } from './CreatePaymentModal'

const PAGE_SIZE = 20

export function OutstandingInvoicesPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<OutstandingInvoiceItem> | null>(null)
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [payingInvoiceId, setPayingInvoiceId] = useState<string | null>(null)

  function reload() {
    setIsLoading(true)
    getOutstandingInvoices({ page, pageSize: PAGE_SIZE })
      .then(setResult)
      .finally(() => setIsLoading(false))
  }

  useEffect(reload, [page])
  useEffect(() => {
    listCustomerNames().then(setCustomerNames)
  }, [])

  return (
    <div>
      <h2 className="mb-4 text-lg font-medium text-gray-900">Outstanding invoices</h2>
      <DataTable
        columns={[
          { header: 'Customer', render: (row: OutstandingInvoiceItem) => customerNames.get(row.customerId) ?? row.customerId },
          { header: 'Invoiced', render: (row: OutstandingInvoiceItem) => formatDate(row.createdAt) },
          { header: 'Total', render: (row: OutstandingInvoiceItem) => formatCurrency(row.total), align: 'right' },
          { header: 'Paid', render: (row: OutstandingInvoiceItem) => formatCurrency(row.paidTotal), align: 'right' },
          { header: 'Outstanding', render: (row: OutstandingInvoiceItem) => formatCurrency(row.outstanding), align: 'right' },
          {
            header: 'Actions',
            render: (row: OutstandingInvoiceItem) => (
              <button type="button" onClick={() => setPayingInvoiceId(row.invoiceId)} className="text-sm text-violet-600 hover:underline">
                Pay
              </button>
            ),
            align: 'right',
          },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.invoiceId}
        isLoading={isLoading}
        emptyMessage="No outstanding invoices."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
      {payingInvoiceId && (
        <CreatePaymentModal
          outstandingInvoices={result?.items ?? []}
          customerNames={customerNames}
          preselectedInvoiceId={payingInvoiceId}
          onClose={() => setPayingInvoiceId(null)}
          onCreated={() => {
            setPayingInvoiceId(null)
            reload()
          }}
        />
      )}
    </div>
  )
}
