export interface LineItem {
  productId: string
  quantity: number
  unitPrice: number
}

interface LineItemsEditorProps {
  productNames: Map<string, string>
  lines: LineItem[]
  onChange: (lines: LineItem[]) => void
  priceLabel?: string
}

const EMPTY_LINE: LineItem = { productId: '', quantity: 1, unitPrice: 0 }

/** Shared by PurchaseRequestsPage and PurchaseOrdersPage - the backend deliberately doesn't require a PO's lines to match its originating request's lines, so both forms just start from this same editable row shape. */
export function LineItemsEditor({ productNames, lines, onChange, priceLabel = 'Unit price' }: LineItemsEditorProps) {
  const productOptions = Array.from(productNames.entries())

  function updateLine(index: number, patch: Partial<LineItem>) {
    onChange(lines.map((line, i) => (i === index ? { ...line, ...patch } : line)))
  }

  function removeLine(index: number) {
    onChange(lines.filter((_, i) => i !== index))
  }

  function addLine() {
    onChange([...lines, { ...EMPTY_LINE }])
  }

  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-500">Lines</label>
      <div className="space-y-2">
        {lines.map((line, index) => (
          <div key={index} className="flex items-end gap-2">
            <select
              required
              value={line.productId}
              onChange={(event) => updateLine(index, { productId: event.target.value })}
              className="flex-1 rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            >
              <option value="" disabled>
                {productOptions.length === 0 ? 'No products yet' : 'Select a product'}
              </option>
              {productOptions.map(([id, name]) => (
                <option key={id} value={id}>
                  {name}
                </option>
              ))}
            </select>
            <input
              type="number"
              min={1}
              required
              value={line.quantity}
              onChange={(event) => updateLine(index, { quantity: Number(event.target.value) })}
              placeholder="Qty"
              className="w-20 rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            />
            <input
              type="number"
              min={0}
              step="0.01"
              required
              value={line.unitPrice}
              onChange={(event) => updateLine(index, { unitPrice: Number(event.target.value) })}
              placeholder={priceLabel}
              className="w-28 rounded-md border border-gray-300 px-2 py-1.5 text-sm"
            />
            <button
              type="button"
              onClick={() => removeLine(index)}
              disabled={lines.length === 1}
              aria-label="Remove line"
              className="px-2 py-1.5 text-gray-400 hover:text-red-600 disabled:opacity-30"
            >
              ✕
            </button>
          </div>
        ))}
      </div>
      <button type="button" onClick={addLine} className="mt-2 text-sm text-violet-600 hover:underline">
        + Add line
      </button>
    </div>
  )
}

export function createEmptyLine(): LineItem {
  return { ...EMPTY_LINE }
}
