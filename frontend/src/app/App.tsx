import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth/AuthProvider'
import { LoginPage } from '../auth/LoginPage'
import { CreateOrganizationPage } from '../auth/CreateOrganizationPage'
import { PasswordResetRequestPage } from '../auth/PasswordResetRequestPage'
import { PasswordResetConfirmPage } from '../auth/PasswordResetConfirmPage'
import { ProtectedRoute } from '../routes/ProtectedRoute'
import { AppLayout } from '../components/AppLayout'
import { DashboardPage } from './DashboardPage'
import { ReportsLayout } from '../features/reports/ReportsLayout'
import { SalesReportPage } from '../features/reports/SalesReportPage'
import { InventoryReportPage } from '../features/reports/InventoryReportPage'
import { LowStockReportPage } from '../features/reports/LowStockReportPage'
import { SlowMovingReportPage } from '../features/reports/SlowMovingReportPage'
import { ProcurementSpendReportPage } from '../features/reports/ProcurementSpendReportPage'
import { OutstandingInvoicesReportPage } from '../features/reports/OutstandingInvoicesReportPage'
import { TopProductsReportPage } from '../features/reports/TopProductsReportPage'
import { InventoryLayout } from '../features/inventory/InventoryLayout'
import { BalancesPage } from '../features/inventory/BalancesPage'
import { LedgerPage } from '../features/inventory/LedgerPage'
import { ProductsPage } from '../features/inventory/ProductsPage'
import { WarehousesPage } from '../features/inventory/WarehousesPage'
import { BranchesPage } from '../features/inventory/BranchesPage'
import { ProcurementLayout } from '../features/procurement/ProcurementLayout'
import { PurchaseRequestsPage } from '../features/procurement/PurchaseRequestsPage'
import { PurchaseOrdersPage } from '../features/procurement/PurchaseOrdersPage'
import { SuppliersPage } from '../features/procurement/SuppliersPage'
import { SalesLayout } from '../features/sales/SalesLayout'
import { CustomersPage } from '../features/sales/CustomersPage'
import { QuotationsPage } from '../features/sales/QuotationsPage'
import { SalesOrdersPage } from '../features/sales/SalesOrdersPage'
import { DeliveriesPage } from '../features/sales/DeliveriesPage'
import { InvoicesPage } from '../features/sales/InvoicesPage'

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
            <Route element={<AppLayout />}>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/inventory" element={<InventoryLayout />}>
                <Route index element={<Navigate to="/inventory/balances" replace />} />
                <Route path="balances" element={<BalancesPage />} />
                <Route path="ledger" element={<LedgerPage />} />
                <Route path="products" element={<ProductsPage />} />
                <Route path="warehouses" element={<WarehousesPage />} />
                <Route path="branches" element={<BranchesPage />} />
              </Route>
              <Route path="/procurement" element={<ProcurementLayout />}>
                <Route index element={<Navigate to="/procurement/requests" replace />} />
                <Route path="requests" element={<PurchaseRequestsPage />} />
                <Route path="orders" element={<PurchaseOrdersPage />} />
                <Route path="suppliers" element={<SuppliersPage />} />
              </Route>
              <Route path="/sales" element={<SalesLayout />}>
                <Route index element={<Navigate to="/sales/customers" replace />} />
                <Route path="customers" element={<CustomersPage />} />
                <Route path="quotations" element={<QuotationsPage />} />
                <Route path="orders" element={<SalesOrdersPage />} />
                <Route path="deliveries" element={<DeliveriesPage />} />
                <Route path="invoices" element={<InvoicesPage />} />
              </Route>
              <Route path="/reports" element={<ReportsLayout />}>
                <Route index element={<Navigate to="/reports/sales" replace />} />
                <Route path="sales" element={<SalesReportPage />} />
                <Route path="inventory" element={<InventoryReportPage />} />
                <Route path="low-stock" element={<LowStockReportPage />} />
                <Route path="slow-moving" element={<SlowMovingReportPage />} />
                <Route path="procurement-spend" element={<ProcurementSpendReportPage />} />
                <Route path="outstanding-invoices" element={<OutstandingInvoicesReportPage />} />
                <Route path="top-products" element={<TopProductsReportPage />} />
              </Route>
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}

export default App
