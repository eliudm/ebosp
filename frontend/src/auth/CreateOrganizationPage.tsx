import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { createTenant } from '../services/identityApi'
import { ApiError } from '../services/apiClient'

/** Self-service tenant onboarding (spec §30: "a new tenant can be created and isolated from other tenants"). */
export function CreateOrganizationPage() {
  const navigate = useNavigate()

  const [tenantName, setTenantName] = useState('')
  const [adminEmail, setAdminEmail] = useState('')
  const [adminPassword, setAdminPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createTenant({ tenantName, adminEmail, adminPassword })
      navigate('/login', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError && err.problem?.detail ? err.problem.detail : 'Could not create the organization.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>Create your organization</h1>
        <form onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="tenantName">Organization name</label>
            <input
              id="tenantName"
              name="tenantName"
              type="text"
              required
              minLength={2}
              value={tenantName}
              onChange={(event) => setTenantName(event.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="adminEmail">Your email</label>
            <input
              id="adminEmail"
              name="adminEmail"
              type="email"
              autoComplete="email"
              required
              value={adminEmail}
              onChange={(event) => setAdminEmail(event.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="adminPassword">Password</label>
            <input
              id="adminPassword"
              name="adminPassword"
              type="password"
              autoComplete="new-password"
              required
              minLength={10}
              value={adminPassword}
              onChange={(event) => setAdminPassword(event.target.value)}
            />
          </div>
          {error && (
            <p role="alert" className="error-text">
              {error}
            </p>
          )}
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creating…' : 'Create organization'}
          </button>
        </form>
        <div className="auth-links">
          <Link to="/login">Already have an account? Sign in</Link>
        </div>
      </div>
    </main>
  )
}
