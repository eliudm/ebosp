import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { requestPasswordReset } from '../services/identityApi'

/** Always shows the same confirmation regardless of outcome (spec §11: no account enumeration). */
export function PasswordResetRequestPage() {
  const [email, setEmail] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSubmitting(true)
    try {
      await requestPasswordReset({ email })
    } finally {
      setIsSubmitting(false)
      setSubmitted(true)
    }
  }

  if (submitted) {
    return (
      <main className="auth-page">
        <div className="auth-card">
          <h1>Check your email</h1>
          <p>If an account exists for that address, we&apos;ve sent instructions to reset the password.</p>
          <div className="auth-links">
            <Link to="/password-reset/confirm">I have a reset code</Link>
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>Reset your password</h1>
        <form onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>
          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Sending…' : 'Send reset instructions'}
          </button>
        </form>
        <div className="auth-links">
          <Link to="/login">Back to sign in</Link>
        </div>
      </div>
    </main>
  )
}
