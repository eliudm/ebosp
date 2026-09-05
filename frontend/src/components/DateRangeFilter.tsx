interface DateRangeFilterProps {
  from: string
  to: string
  onFromChange: (value: string) => void
  onToChange: (value: string) => void
  onApply: () => void
  isLoading?: boolean
}

/** Plain yyyy-mm-dd date inputs - the caller converts to ISO before calling the report API, and treats both empty as "use the backend's own default (last 30 days)". */
export function DateRangeFilter({ from, to, onFromChange, onToChange, onApply, isLoading }: DateRangeFilterProps) {
  return (
    <form
      className="mb-4 flex flex-wrap items-end gap-3"
      onSubmit={(event) => {
        event.preventDefault()
        onApply()
      }}
    >
      <div>
        <label htmlFor="from" className="mb-1 block text-xs font-medium text-gray-500">
          From
        </label>
        <input
          id="from"
          type="date"
          value={from}
          onChange={(event) => onFromChange(event.target.value)}
          className="rounded-md border border-gray-300 px-2 py-1.5 text-sm"
        />
      </div>
      <div>
        <label htmlFor="to" className="mb-1 block text-xs font-medium text-gray-500">
          To
        </label>
        <input
          id="to"
          type="date"
          value={to}
          onChange={(event) => onToChange(event.target.value)}
          className="rounded-md border border-gray-300 px-2 py-1.5 text-sm"
        />
      </div>
      <button
        type="submit"
        disabled={isLoading}
        className="rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-60"
      >
        {isLoading ? 'Loading…' : 'Apply'}
      </button>
    </form>
  )
}
