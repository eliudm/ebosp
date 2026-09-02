import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth/AuthProvider'
import { LoginPage } from '../auth/LoginPage'
import { CreateOrganizationPage } from '../auth/CreateOrganizationPage'
import { PasswordResetRequestPage } from '../auth/PasswordResetRequestPage'
import { PasswordResetConfirmPage } from '../auth/PasswordResetConfirmPage'
import { ProtectedRoute } from '../routes/ProtectedRoute'
import { DashboardPage } from './DashboardPage'

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/create-organization" element={<CreateOrganizationPage />} />
          <Route path="/password-reset/request" element={<PasswordResetRequestPage />} />
          <Route path="/password-reset/confirm" element={<PasswordResetConfirmPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<DashboardPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
