import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { Pagination } from '../../components/Pagination'
import { ApiError, getErrorMessage } from '../../services/apiClient'
import { confirmPayment, failPayment, getOutstandingInvoices, listPayments } from '../../services/financeApi'
import { listCustomerNames } from '../../services/lookupApi'
import type { PagedResult } from '../../types/common'
import type { PaymentResponse } from '../../types/finance'
import type { OutstandingInvoiceItem } from '../../types/reporting'
import { formatCurrency, formatDate } from '../../utils/format'
import { CreatePaymentModal } from './CreatePaymentModal'

const PAGE_SIZE = 20

export function PaymentsPage() {
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<PaymentResponse> | null>(null)
  const [outstandingInvoices, setOutstandingInvoices] = useState<OutstandingInvoiceItem[]>([])
  const [customerNames, setCustomerNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)

  function reload() {
    setIsLoading(true)
    listPayments({ page, pageSize: PAGE_SIZE, sortDescending: true })
      .then(setResult)
      .finally(() => setIsLoading(false))
  }

  function reloadOutstandingInvoices() {
    getOutstandingInvoices({ pageSize: 100 }).then((result) => setOutstandingInvoices(result.items))
  }

  useEffect(reload, [page])
  useEffect(() => {
    listCustomerNames().then(setCustomerNames)
    reloadOutstandingInvoices()
  }, [])

  async function handleConfirm(payment: PaymentResponse) {
    setActionError(null)
    try {
      await confirmPayment(payment.id)
      reload()
      reloadOutstandingInvoices()
    } catch (err) {
      setActionError(describeConfirmError(err))
    }
  }

  async function handleFail(payment: PaymentResponse) {
    setActionError(null)
    try {
      await failPayment(payment.id)
      reload()
    } catch (err) {
      setActionError(getErrorMessage(err, 'Could not fail this payment.'))
    }
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Payments</h2>
        <button
          type="button"
          onClick={() => setIsModalOpen(true)}
          className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700"
        >
          New payment
        </button>
      </div>
      {actionError && <p className="mb-4 text-sm text-red-600">{actionError}</p>}
      <DataTable
        columns={[
          { header: 'Invoice', render: (row: PaymentResponse) => row.invoiceId.slice(0, 8) },
          { header: 'Amount', render: (row: PaymentResponse) => formatCurrency(row.amount), align: 'right' },
          { header: 'Method', render: (row: PaymentResponse) => row.method },
          { header: 'Status', render: (row: PaymentResponse) => row.status },
          { header: 'Confirmed', render: (row: PaymentResponse) => formatDate(row.confirmedAt) },
          {
            header: 'Actions',
            render: (row: PaymentResponse) =>
              row.status === 'Pending' ? (
                <div className="flex justify-end gap-2">
                  <button type="button" onClick={() => handleConfirm(row)} className="text-sm text-violet-600 hover:underline">
                    Confirm
                  </button>
                  <button type="button" onClick={() => handleFail(row)} className="text-sm text-red-600 hover:underline">
                    Fail
                  </button>
                </div>
              ) : (
                '—'
              ),
            align: 'right',
          },
        ]}
        rows={result?.items ?? []}
        keyFor={(row) => row.id}
        isLoading={isLoading}
        emptyMessage="No payments yet."
      />
      {result && <Pagination page={result.page} totalPages={result.totalPages} onPageChange={setPage} />}
      {isModalOpen && (
        <CreatePaymentModal
          outstandingInvoices={outstandingInvoices}
          customerNames={customerNames}
          onClose={() => setIsModalOpen(false)}
          onCreated={() => {
            setIsModalOpen(false)
            reload()
            reloadOutstandingInvoices()
          }}
        />
      )}
    </div>
  )
}

function describeConfirmError(err: unknown): string {
  if (err instanceof ApiError) {
    // A real typed exception (already confirmed/failed, overpayment, non-issued invoice) comes
    // back with a message in problem.title. A bare 403 with no title means the amount exceeded
    // BillingOptions.LargePaymentThreshold and the caller lacks payment.create.large (only
    // tenant-admin holds it - Finance role holds payment.create but not the large tier).
    if (err.problem?.title) {
      return err.problem.title
    }
    if (err.status === 403) {
      return 'This payment is large enough to need additional permission, which this account does not have.'
    }
  }
  return 'Could not confirm this payment.'
}
