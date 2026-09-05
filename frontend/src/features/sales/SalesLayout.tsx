import { NavLink, Outlet } from 'react-router-dom'

const tabs = [
  { to: '/sales/customers', label: 'Customers' },
  { to: '/sales/quotations', label: 'Quotations' },
  { to: '/sales/orders', label: 'Orders' },
  { to: '/sales/deliveries', label: 'Deliveries' },
  { to: '/sales/invoices', label: 'Invoices' },
]

function tabClassName({ isActive }: { isActive: boolean }): string {
  return [
    'whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium',
    isActive ? 'border-violet-600 text-violet-700' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
  ].join(' ')
}

/** Same tab-strip shape as ReportsLayout/InventoryLayout/ProcurementLayout - one tab per stage of the customer -> quotation -> order -> delivery -> invoice chain. */
export function SalesLayout() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Sales</h1>
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
