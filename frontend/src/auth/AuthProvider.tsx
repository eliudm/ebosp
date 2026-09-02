import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { configureApiClient } from '../services/apiClient'
import * as identityApi from '../services/identityApi'
import { AuthContext } from './authContext'
import type { AuthContextValue } from './authContext'

interface AuthState {
  status: 'unauthenticated' | 'authenticated'
  email: string | null
}

interface Tokens {
  accessToken: string
  refreshToken: string
}

/**
 * Holds the session in memory only - never in localStorage/sessionStorage (dev guide §11.1 step
 * 6: "Frontend stores only what the security model permits; avoid unsafe token storage
 * patterns"). A page reload ends the session; a real deployment would trade that for an httpOnly
 * refresh-token cookie, which needs backend support this pass doesn't add yet.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'unauthenticated', email: null })
  const tokensRef = useRef<Tokens | null>(null)

  const clearSession = useCallback(() => {
    tokensRef.current = null
    setState({ status: 'unauthenticated', email: null })
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const tokens = await identityApi.login({ email, password })
    tokensRef.current = { accessToken: tokens.accessToken, refreshToken: tokens.refreshToken }
    setState({ status: 'authenticated', email })
  }, [])

  const logout = useCallback(async () => {
    const tokens = tokensRef.current
    clearSession()
    if (tokens) {
      try {
        await identityApi.logout({ refreshToken: tokens.refreshToken })
      } catch {
        // Best-effort - the client-side session is already cleared either way.
      }
    }
  }, [clearSession])

  useEffect(() => {
    configureApiClient({
      getAccessToken: () => tokensRef.current?.accessToken ?? null,
      refresh: async () => {
        const current = tokensRef.current
        if (!current) {
          return false
        }
        try {
          const tokens = await identityApi.refresh({ refreshToken: current.refreshToken })
          tokensRef.current = { accessToken: tokens.accessToken, refreshToken: tokens.refreshToken }
          return true
        } catch {
          return false
        }
      },
      onSessionExpired: clearSession,
    })
  }, [clearSession])

  const value = useMemo<AuthContextValue>(() => ({ ...state, login, logout }), [state, login, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
