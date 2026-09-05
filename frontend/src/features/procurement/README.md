# Procurement feature

Purchase requests, approvals, purchase orders, and suppliers (dev guide §14), backed by
`PurchaseRequestsController`/`PurchaseOrdersController`/`SuppliersController`. `ProcurementLayout.tsx`
renders it as tabs under `/procurement/*`. Creating a purchase order pre-fills its lines from the
selected approved request (editable, since the backend doesn't require them to match). Goods receipt
itself stays in the Inventory feature (`ReceiveStockForm.tsx`), which now optionally links a receipt
to a purchase order.

**Scoped out of this pass**: edit/activate/deactivate for suppliers (list + create only, same cut as
Branches/Warehouses/Products).
