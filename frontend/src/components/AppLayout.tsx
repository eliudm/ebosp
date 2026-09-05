import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/inventory', label: 'Inventory', end: false },
  { to: '/procurement', label: 'Procurement', end: false },
  { to: '/sales', label: 'Sales', end: false },
  { to: '/reports', label: 'Reports', end: false },
]

function navLinkClassName({ isActive }: { isActive: boolean }): string {
  return [
    'block rounded-md px-3 py-2 text-sm font-medium transition-colors',
    isActive ? 'bg-violet-50 text-violet-700' : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900',
  ].join(' ')
}

/** Shared shell (sidebar nav + top bar) for every authenticated page - see docs on ReportsLayout for why this exists now rather than per-page headers. */
export function AppLayout() {
  const { email, logout } = useAuth()

  return (
    <div className="flex min-h-svh">
      <aside className="w-56 shrink-0 border-r border-gray-200 bg-white">
        <div className="px-4 py-5">
          <span className="text-lg font-bold tracking-tight text-gray-900">EBOSP</span>
        </div>
        <nav className="space-y-1 px-2">
          {navItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.end} className={navLinkClassName}>
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm text-gray-500">Signed in as {email}</span>
          <button
            type="button"
            onClick={() => void logout()}
            className="rounded-md border border-gray-300 px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-50"
          >
            Sign out
          </button>
        </header>
        <main className="flex-1 bg-gray-50 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
