import { NavLink, Outlet } from 'react-router-dom'

const tabs = [
  { to: '/reports/sales', label: 'Sales' },
  { to: '/reports/inventory', label: 'Inventory' },
  { to: '/reports/low-stock', label: 'Low Stock' },
  { to: '/reports/slow-moving', label: 'Slow Moving' },
  { to: '/reports/procurement-spend', label: 'Procurement Spend' },
  { to: '/reports/outstanding-invoices', label: 'Outstanding Invoices' },
  { to: '/reports/top-products', label: 'Top Products' },
]

function tabClassName({ isActive }: { isActive: boolean }): string {
  return [
    'whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium',
    isActive ? 'border-violet-600 text-violet-700' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
  ].join(' ')
}

/** Seven read-only reports (spec §8.6/§19) read more like tabs within one Reports area than seven separate app sections. */
export function ReportsLayout() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Reports</h1>
      <div className="mb-6 flex gap-4 overflow-x-auto border-b border-gray-200">
        {tabs.map((tab) => (
          <NavLink key={tab.to} to={tab.to} className={tabClassName}>
            {tab.label}
          </NavLink>
        ))}
      </div>
      <Outlet />
    </div>
  )
}
