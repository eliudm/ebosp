import { useAuth } from '../auth/useAuth'

/** Placeholder landing page behind ProtectedRoute - feature modules replace this from Phase 3 onward. */
export function DashboardPage() {
  const { email, logout } = useAuth()

  return (
    <main>
      <h1>EBOSP</h1>
      <p>Signed in as {email}.</p>
      <button type="button" onClick={() => void logout()}>
        Sign out
      </button>
    </main>
  )
}
