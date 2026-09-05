import { NavLink, Outlet } from 'react-router-dom'

const tabs = [
  { to: '/procurement/requests', label: 'Purchase Requests' },
  { to: '/procurement/orders', label: 'Purchase Orders' },
  { to: '/procurement/suppliers', label: 'Suppliers' },
]

function tabClassName({ isActive }: { isActive: boolean }): string {
  return [
    'whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium',
    isActive ? 'border-violet-600 text-violet-700' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
  ].join(' ')
}

/** Same tab-strip shape as ReportsLayout/InventoryLayout. */
export function ProcurementLayout() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Procurement</h1>
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
