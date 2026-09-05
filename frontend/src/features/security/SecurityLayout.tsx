import { NavLink, Outlet } from 'react-router-dom'

const tabs = [
  { to: '/security/audit-events', label: 'Audit Events' },
  { to: '/security/alerts', label: 'Security Alerts' },
]

function tabClassName({ isActive }: { isActive: boolean }): string {
  return [
    'whitespace-nowrap border-b-2 px-3 py-2 text-sm font-medium',
    isActive ? 'border-violet-600 text-violet-700' : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700',
  ].join(' ')
}

/** Same tab-strip shape as every other module. audit.read and security.alert.manage are disjoint permissions (only tenant-admin holds both), so both tabs are always shown - a non-admin user just sees a real 403 message on whichever one they lack, rather than the UI guessing and hiding a tab. */
export function SecurityLayout() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Security</h1>
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
