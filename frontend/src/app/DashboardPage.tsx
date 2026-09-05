import { Link } from 'react-router-dom'

/** Landing page behind AppLayout/ProtectedRoute - more feature modules replace this over time. */
export function DashboardPage() {
  return (
    <div>
      <h1 className="mb-4 text-xl font-semibold text-gray-900">Dashboard</h1>
      <p className="text-gray-600">
        <Link to="/reports" className="text-violet-600 hover:underline">
          View reports
        </Link>
      </p>
    </div>
  )
}
