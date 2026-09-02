import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

/** Redirects to /login when unauthenticated (dev guide §11.1: route guards are UX-only - the API is the real authority). */
export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status !== 'authenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}
