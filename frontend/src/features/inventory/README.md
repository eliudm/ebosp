# Inventory feature

Products, stock ledger, balances, receipts, transfers, adjustments (dev guide §13), backed by
`InventoryController`/`ProductsController`/`WarehousesController`/`BranchesController`.
`InventoryLayout.tsx` renders it as tabs under `/inventory/*`: Balances (with Receive/Issue/
Transfer/Adjust actions, each a `Modal`-wrapped form), Ledger, Products, Warehouses, Branches.

**Scoped out of this pass** (real backend features, just not built here yet): product category
assignment (products are created uncategorized), stock reservations, reorder rule management, and
edit/activate/deactivate for products/warehouses/branches (list + create only).
