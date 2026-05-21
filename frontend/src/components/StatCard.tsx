import type { LucideIcon } from 'lucide-react'

interface StatCardProps {
  label: string
  value: string
  description: string
  icon: LucideIcon
}

export function StatCard({
  label,
  value,
  description,
  icon: Icon,
}: StatCardProps) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-semibold text-slate-500">{label}</p>
          <p className="mt-2 text-3xl font-bold text-slate-950">{value}</p>
        </div>
        <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-care-50 text-care-700">
          <Icon className="h-5 w-5" aria-hidden="true" />
        </span>
      </div>
      <p className="mt-4 text-sm leading-6 text-slate-600">{description}</p>
    </div>
  )
}
