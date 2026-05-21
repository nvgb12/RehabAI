import type { ReactNode } from 'react'

interface DashboardLayoutProps {
  title: string
  description: string
  children: ReactNode
}

export function DashboardLayout({
  title,
  description,
  children,
}: DashboardLayoutProps) {
  return (
    <section className="bg-slate-50 py-10 sm:py-14">
      <div className="page-container">
        <div className="mb-8 max-w-3xl">
          <h1 className="text-3xl font-bold text-slate-950 sm:text-4xl">
            {title}
          </h1>
          <p className="mt-3 text-base leading-7 text-slate-600">
            {description}
          </p>
        </div>
        {children}
      </div>
    </section>
  )
}
