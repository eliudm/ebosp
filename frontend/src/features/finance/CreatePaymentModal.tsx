import { useState } from 'react'
import type { FormEvent } from 'react'
import { Modal } from '../../components/Modal'
import { getErrorMessage } from '../../services/apiClient'
import { createPayment } from '../../services/financeApi'
import type { OutstandingInvoiceItem } from '../../types/reporting'
import { formatCurrency } from '../../utils/format'

interface Props {
  outstandingInvoices: OutstandingInvoiceItem[]
  customerNames: Map<string, string>
  preselectedInvoiceId?: string
  onClose: () => void
  onCreated: () => void
}

/** Shared by PaymentsPage's "New payment" button and OutstandingInvoicesPage's per-row "Pay" button. */
export function CreatePaymentModal({ outstandingInvoices, customerNames, preselectedInvoiceId, onClose, onCreated }: Props) {
  const [invoiceId, setInvoiceId] = useState(preselectedInvoiceId ?? '')
  const [amount, setAmount] = useState(() => {
    const preselected = outstandingInvoices.find((i) => i.invoiceId === preselectedInvoiceId)
    return preselected ? String(preselected.outstanding) : '0'
  })
  const [method, setMethod] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleInvoiceChange(id: string) {
    setInvoiceId(id)
    const invoice = outstandingInvoices.find((i) => i.invoiceId === id)
    if (invoice) {
      setAmount(String(invoice.outstanding))
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createPayment({ invoiceId, amount: Number(amount), method, idempotencyKey: crypto.randomUUID() })
      onCreated()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not create the payment.'))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal title="New payment" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Outstanding invoice</label>
          <select
            required
            value={invoiceId}
            onChange={(event) => handleInvoiceChange(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          >
            <option value="" disabled>
              {outstandingInvoices.length === 0 ? 'No outstanding invoices' : 'Select an invoice'}
            </option>
            {outstandingInvoices.map((invoice) => (
              <option key={invoice.invoiceId} value={invoice.invoiceId}>
                {customerNames.get(invoice.customerId) ?? invoice.customerId} — {formatCurrency(invoice.outstanding)} outstanding
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Amount</label>
          <input
            type="number"
            min={0.01}
            step="0.01"
            required
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-500">Method</label>
          <input
            required
            value={method}
            onChange={(event) => setMethod(event.target.value)}
            placeholder="e.g. Cash, BankTransfer, Card"
            className="w-full rounded-md border border-gray-300 px-2 py-1.5 text-sm"
          />
        </div>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-md bg-violet-600 px-3 py-2 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
        >
          {isSubmitting ? 'Creating…' : 'Create payment'}
        </button>
      </form>
    </Modal>
  )
}
