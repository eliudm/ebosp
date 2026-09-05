const currencyFormatter = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' })
const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })
const numberFormatter = new Intl.NumberFormat()

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }
  return dateFormatter.format(new Date(value))
}

export function formatNumber(value: number): string {
  return numberFormatter.format(value)
}
