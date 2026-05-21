interface StatusBadgeProps {
  value: string
}

const toneByStatus: Record<string, string> = {
  Active: 'bg-rehab-50 text-rehab-800 border-rehab-200',
  Paid: 'bg-rehab-50 text-rehab-800 border-rehab-200',
  Confirmed: 'bg-rehab-50 text-rehab-800 border-rehab-200',
  Processing: 'bg-care-50 text-care-800 border-care-200',
  Shipped: 'bg-care-50 text-care-800 border-care-200',
  Pending: 'bg-amber-50 text-amber-800 border-amber-200',
  PendingPayment: 'bg-amber-50 text-amber-800 border-amber-200',
  Inactive: 'bg-slate-100 text-slate-700 border-slate-200',
  Completed: 'bg-slate-100 text-slate-700 border-slate-200',
  Cancelled: 'bg-red-50 text-red-700 border-red-200',
  Failed: 'bg-red-50 text-red-700 border-red-200',
}

export function StatusBadge({ value }: StatusBadgeProps) {
  const tone = toneByStatus[value] ?? 'bg-slate-100 text-slate-700 border-slate-200'

  return (
    <span
      className={`inline-flex rounded-lg border px-2.5 py-1 text-xs font-bold ${tone}`}
    >
      {value}
    </span>
  )
}
