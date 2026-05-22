import type { ReactNode } from 'react'
import {
  CalendarDays,
  ClipboardList,
  LayoutDashboard,
  Stethoscope,
  UserRoundCog,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'

interface DoctorLayoutProps {
  title: string
  description: string
  children: ReactNode
}

const doctorNavItems = [
  { to: '/doctor/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/doctor/profile', label: 'Profile', icon: UserRoundCog },
  { to: '/doctor/schedule', label: 'Schedule', icon: CalendarDays },
  { to: '/doctor/appointments', label: 'Appointments', icon: ClipboardList },
]

function doctorNavClass({ isActive }: { isActive: boolean }) {
  return [
    'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold transition',
    isActive
      ? 'bg-care-50 text-care-800'
      : 'text-slate-600 hover:bg-slate-50 hover:text-care-800',
  ].join(' ')
}

export function DoctorLayout({
  title,
  description,
  children,
}: DoctorLayoutProps) {
  return (
    <section className="bg-slate-50 py-8 sm:py-10">
      <div className="page-container">
        <div className="grid gap-6 lg:grid-cols-[240px_1fr]">
          <aside className="h-fit rounded-lg border border-slate-200 bg-white p-3 shadow-sm">
            <div className="px-3 py-2">
              <p className="flex items-center gap-2 text-xs font-bold uppercase tracking-wide text-care-700">
                <Stethoscope className="h-4 w-4" aria-hidden="true" />
                Doctor
              </p>
              <p className="mt-1 text-sm text-slate-500">
                Stroke rehab workspace
              </p>
            </div>
            <nav className="mt-3 grid gap-1">
              {doctorNavItems.map((item) => (
                <NavLink key={item.to} to={item.to} className={doctorNavClass}>
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
