import { LoaderCircle } from 'lucide-react'

interface LoadingStateProps {
  label?: string
}

export function LoadingState({ label = 'Đang tải dữ liệu' }: LoadingStateProps) {
  return (
    <div className="flex min-h-48 items-center justify-center rounded-lg border border-dashed border-slate-200 bg-white p-8 text-slate-600">
      <LoaderCircle className="mr-3 h-5 w-5 animate-spin" aria-hidden="true" />
      <span className="text-sm font-semibold">{label}</span>
    </div>
  )
}
