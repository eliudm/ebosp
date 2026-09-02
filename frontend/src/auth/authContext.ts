import { createContext } from 'react'

export interface AuthContextValue {
  status: 'unauthenticated' | 'authenticated'
  email: string | null
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
