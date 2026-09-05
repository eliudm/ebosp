import { NavLink, Outlet } from 'react-router-dom'

const tabs = [
  { to: '/inventory/balances', label: 'Balances' },
  { to: '/inventory/ledger', label: 'Ledger' },
  { to: '/inventory/products', label: 'Products' },
  { to: '/inventory/warehouses', label: 'Warehouses' },
  { to: '/inventory/branches', label: 'Branches' },
]

function tabClassName({ isActive }: { isActive: boolean }): string {
  return [
    'whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium',
    isActive ? 'border-violet-600 text-violet-700' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
  ].join(' ')
}

/** Same tab-strip shape as ReportsLayout.tsx - products/warehouses/branches are prerequisites for the operational tabs (balances/ledger), not a separate feature area. */
export function InventoryLayout() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Inventory</h1>
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
