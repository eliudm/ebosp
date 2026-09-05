import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { confirmPasswordReset } from '../services/identityApi'

export function PasswordResetConfirmPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const [token, setToken] = useState(searchParams.get('token') ?? '')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await confirmPasswordReset({ token, newPassword })
      navigate('/login', { replace: true })
    } catch {
      setError('That reset code is invalid or has expired.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>Set a new password</h1>
        <form onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="token">Reset code</label>
            <input id="token" name="token" type="text" required value={token} onChange={(event) => setToken(event.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="newPassword">New password</label>
            <input
              id="newPassword"
              name="newPassword"
              type="password"
              autoComplete="new-password"
              required
              minLength={10}
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
            />
          </div>
          {error && (
            <p role="alert" className="error-text">
              {error}
            </p>
          )}
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Set new password'}
          </button>
        </form>
        <div className="auth-links">
          <Link to="/login">Back to sign in</Link>
        </div>
      </div>
    </main>
  )
}
