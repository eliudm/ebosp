import type { ReactNode } from 'react'

export interface DataTableColumn<T> {
  header: string
  render: (row: T) => ReactNode
  align?: 'left' | 'right'
}

interface DataTableProps<T> {
  columns: DataTableColumn<T>[]
  rows: T[]
  keyFor: (row: T) => string
  isLoading?: boolean
  emptyMessage?: string
}

export function DataTable<T>({ columns, rows, keyFor, isLoading, emptyMessage = 'No data for this view yet.' }: DataTableProps<T>) {
  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white shadow-sm">
      <table className="min-w-full divide-y divide-gray-200 text-sm">
        <thead className="bg-gray-50">
          <tr>
            {columns.map((column) => (
              <th
                key={column.header}
                className={`px-4 py-2 font-medium text-gray-500 ${column.align === 'right' ? 'text-right' : 'text-left'}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {isLoading ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-6 text-center text-gray-400">
                Loading…
              </td>
            </tr>
          ) : rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-6 text-center text-gray-400">
                {emptyMessage}
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr key={keyFor(row)} className="hover:bg-gray-50">
                {columns.map((column) => (
                  <td key={column.header} className={`px-4 py-2 text-gray-700 ${column.align === 'right' ? 'text-right' : 'text-left'}`}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
