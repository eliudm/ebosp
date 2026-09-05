// Mirrors src/EBOSP.Contracts/Sales/*.cs and src/EBOSP.Contracts/Billing/{CreateInvoiceRequest,InvoiceResponse}.cs.
// Invoicing lives here (not a separate "billing" types file) because the Sales feature's own README
// groups it under Sales - only payments (Finance's job) are out of scope.

export interface CreateCustomerRequest {
  name: string
  creditLimit: number
}

export interface CustomerResponse {
  id: string
  name: string
  creditLimit: number
  status: string
}

export interface QuotationLine {
  productId: string
  quantity: number
  unitPrice: number
}

export interface CreateQuotationRequest {
  customerId: string
  branchId: string
  lines: QuotationLine[]
}

export interface QuotationResponse {
  id: string
  customerId: string
  branchId: string
  total: number
  status: 'Pending' | 'Accepted' | 'Rejected'
  lines: QuotationLine[]
}

export interface CreateSalesOrderRequest {
  quotationId: string
  warehouseId: string
}

export interface SalesOrderLine {
  productId: string
  quantity: number
  unitPrice: number
}

export interface SalesOrderResponse {
  id: string
  quotationId: string
  customerId: string
  branchId: string
  warehouseId: string
  status: 'Open' | 'Cancelled' | 'Fulfilled'
  total: number
  createdByUserId: string
  createdAt: string
  lines: SalesOrderLine[]
}

export interface CreateDeliveryRequest {
  salesOrderId: string
}

export interface DeliveryLine {
  productId: string
  quantity: number
}

export interface DeliveryResponse {
  id: string
  salesOrderId: string
  warehouseId: string
  deliveredByUserId: string
  deliveredAt: string
  lines: DeliveryLine[]
}

export interface CreateInvoiceRequest {
  salesOrderId: string
}

export interface InvoiceResponse {
  id: string
  salesOrderId: string
  customerId: string
  total: number
  status: 'Issued' | 'Paid' | 'Cancelled'
  createdByUserId: string
  createdAt: string
}
