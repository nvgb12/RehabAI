import { AlertTriangle } from 'lucide-react'

interface ErrorStateProps {
  title?: string
  message: string
}

export function ErrorState({
  title = 'Không thể tải dữ liệu',
  message,
}: ErrorStateProps) {
  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50 p-5 text-amber-900">
      <div className="flex gap-3">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true" />
        <div>
          <h2 className="text-sm font-bold">{title}</h2>
          <p className="mt-1 text-sm leading-6">{message}</p>
        </div>
      </div>
    </div>
  )
}
