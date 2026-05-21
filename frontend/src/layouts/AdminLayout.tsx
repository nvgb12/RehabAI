import type { ReactNode } from 'react'
import {
  BarChart3,
  ClipboardList,
  LayoutDashboard,
  PackagePlus,
  Stethoscope,
  UserRoundPlus,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'

interface AdminLayoutProps {
  title: string
  description: string
  children: ReactNode
}

const adminNavItems = [
  { to: '/admin/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/admin/products', label: 'Products', icon: PackagePlus },
  { to: '/admin/orders', label: 'Orders', icon: ClipboardList },
  { to: '/admin/reports', label: 'Reports', icon: BarChart3 },
  { to: '/admin/services', label: 'Services', icon: Stethoscope },
  { to: '/admin/doctors', label: 'Doctors', icon: UserRoundPlus },
]

function adminNavClass({ isActive }: { isActive: boolean }) {
  return [
    'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold transition',
    isActive
      ? 'bg-care-50 text-care-800'
      : 'text-slate-600 hover:bg-slate-50 hover:text-care-800',
  ].join(' ')
}

export function AdminLayout({
  title,
  description,
  children,
}: AdminLayoutProps) {
  return (
    <section className="bg-slate-50 py-8 sm:py-10">
      <div className="page-container">
        <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
          <aside className="h-fit rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
            <div className="px-3 py-2">
              <p className="text-xs font-bold uppercase tracking-wide text-care-700">
                Admin
              </p>
              <p className="mt-1 text-sm text-slate-500">
                RehabAI management
              </p>
            </div>
            <nav className="mt-3 grid gap-1">
              {adminNavItems.map((item) => (
                <NavLink key={item.to} to={item.to} className={adminNavClass}>
                  <item.icon className="h-4 w-4" aria-hidden="true" />
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </aside>

          <main>
            <div className="mb-6 max-w-3xl">
              <h1 className="text-3xl font-bold text-slate-950 sm:text-4xl">
                {title}
              </h1>
              <p className="mt-3 text-base leading-7 text-slate-600">
                {description}
              </p>
            </div>
            {children}
          </main>
        </div>
      </div>
    </section>
  )
}
