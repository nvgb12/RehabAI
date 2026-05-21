import type { LucideIcon } from 'lucide-react'
import { SearchX } from 'lucide-react'

interface EmptyStateProps {
  title: string
  message: string
  icon?: LucideIcon
}

export function EmptyState({
  title,
  message,
  icon: Icon = SearchX,
}: EmptyStateProps) {
  return (
    <div className="rounded-lg border border-dashed border-slate-200 bg-white p-8 text-center">
      <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
        <Icon className="h-6 w-6" aria-hidden="true" />
      </span>
      <h2 className="mt-4 text-lg font-bold text-slate-950">{title}</h2>
      <p className="mt-2 text-sm leading-6 text-slate-600">{message}</p>
    </div>
  )
}
