import { useEffect, useState } from 'react'
import { DataTable } from '../../components/DataTable'
import { listProductNames, listWarehouseNames } from '../../services/lookupApi'
import { listBalances } from '../../services/inventoryApi'
import type { StockBalanceResponse } from '../../types/inventory'
import { AdjustStockForm } from './AdjustStockForm'
import { IssueStockForm } from './IssueStockForm'
import { ReceiveStockForm } from './ReceiveStockForm'
import { TransferStockForm } from './TransferStockForm'

type MovementAction = 'receive' | 'issue' | 'transfer' | 'adjust' | null

export function BalancesPage() {
  const [balances, setBalances] = useState<StockBalanceResponse[]>([])
  const [productNames, setProductNames] = useState<Map<string, string>>(new Map())
  const [warehouseNames, setWarehouseNames] = useState<Map<string, string>>(new Map())
  const [isLoading, setIsLoading] = useState(true)
  const [activeAction, setActiveAction] = useState<MovementAction>(null)

  function reload() {
    setIsLoading(true)
    listBalances({ pageSize: 100 })
      .then((result) => setBalances(result.items))
      .finally(() => setIsLoading(false))
  }

  function reloadNames() {
    listProductNames().then(setProductNames)
    listWarehouseNames().then(setWarehouseNames)
  }

  useEffect(reload, [])
  useEffect(reloadNames, [])

  function handleMovementSuccess() {
    setActiveAction(null)
    reload()
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-medium text-gray-900">Stock balances</h2>
        <div className="flex gap-2">
          <MovementButton label="Receive" onClick={() => setActiveAction('receive')} />
          <MovementButton label="Issue" onClick={() => setActiveAction('issue')} />
          <MovementButton label="Transfer" onClick={() => setActiveAction('transfer')} />
          <MovementButton label="Adjust" onClick={() => setActiveAction('adjust')} />
        </div>
      </div>
      <DataTable
        columns={[
          { header: 'Product', render: (row: StockBalanceResponse) => productNames.get(row.productId) ?? row.productId },
          { header: 'Warehouse', render: (row: StockBalanceResponse) => warehouseNames.get(row.warehouseId) ?? row.warehouseId },
          { header: 'On hand', render: (row: StockBalanceResponse) => row.quantityOnHand, align: 'right' },
          { header: 'Reserved', render: (row: StockBalanceResponse) => row.quantityReserved, align: 'right' },
          { header: 'Available', render: (row: StockBalanceResponse) => row.quantityAvailable, align: 'right' },
        ]}
        rows={balances}
        keyFor={(row) => `${row.warehouseId}-${row.productId}`}
        isLoading={isLoading}
        emptyMessage="No stock movements yet - receive some stock to get started."
      />

      {activeAction === 'receive' && (
        <ReceiveStockForm
          warehouseNames={warehouseNames}
          productNames={productNames}
          onClose={() => setActiveAction(null)}
          onSuccess={handleMovementSuccess}
        />
      )}
      {activeAction === 'issue' && (
        <IssueStockForm
          warehouseNames={warehouseNames}
          productNames={productNames}
          onClose={() => setActiveAction(null)}
          onSuccess={handleMovementSuccess}
        />
      )}
      {activeAction === 'transfer' && (
        <TransferStockForm
          warehouseNames={warehouseNames}
          productNames={productNames}
          onClose={() => setActiveAction(null)}
          onSuccess={handleMovementSuccess}
        />
      )}
      {activeAction === 'adjust' && (
        <AdjustStockForm
          warehouseNames={warehouseNames}
          productNames={productNames}
          onClose={() => setActiveAction(null)}
          onSuccess={handleMovementSuccess}
        />
      )}
    </div>
  )
}

function MovementButton({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button type="button" onClick={onClick} className="rounded-md border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50">
      {label}
    </button>
  )
}
