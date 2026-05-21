import {
  Activity,
  CalendarCheck,
  LayoutDashboard,
  LogOut,
  Menu,
  PackageSearch,
  Stethoscope,
  UserRoundPlus,
} from 'lucide-react'
import { useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { clearAuth, getStoredAuth } from '../utils/authStorage'

const navItems = [
  { to: '/products', label: 'Sản phẩm', icon: PackageSearch },
  { to: '/doctors', label: 'Bác sĩ', icon: Stethoscope },
]

function navLinkClass({ isActive }: { isActive: boolean }) {
  return [
    'inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold transition',
    isActive
      ? 'bg-care-50 text-care-800'
      : 'text-slate-600 hover:bg-slate-50 hover:text-care-800',
  ].join(' ')
}

export function AppHeader() {
  const [isOpen, setIsOpen] = useState(false)
  const navigate = useNavigate()
  const session = getStoredAuth()

  const dashboardPath = session?.roles.includes('Admin')
    ? '/admin/dashboard'
    : '/patient/dashboard'

  function handleLogout() {
    clearAuth()
    navigate('/')
  }

  return (
    <header className="sticky top-0 z-40 border-b border-slate-100 bg-white/95 backdrop-blur">
      <div className="page-container">
        <div className="flex h-16 items-center justify-between gap-4">
          <Link to="/" className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-rehab-700 text-white shadow-soft">
              <Activity className="h-5 w-5" aria-hidden="true" />
            </span>
            <span>
              <span className="block text-lg font-bold text-slate-950">
                RehabAI
              </span>
              <span className="block text-xs font-medium text-rehab-700">
                Stroke rehabilitation care
              </span>
            </span>
          </Link>

          <nav className="hidden items-center gap-1 md:flex">
            {navItems.map((item) => (
              <NavLink key={item.to} to={item.to} className={navLinkClass}>
                <item.icon className="h-4 w-4" aria-hidden="true" />
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="hidden items-center gap-2 md:flex">
            {session ? (
              <>
                <Link to={dashboardPath} className="btn-secondary py-2">
                  <LayoutDashboard className="h-4 w-4" aria-hidden="true" />
                  Dashboard
                </Link>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="btn-secondary py-2"
                >
                  <LogOut className="h-4 w-4" aria-hidden="true" />
                  Đăng xuất
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="btn-secondary py-2">
                  <CalendarCheck className="h-4 w-4" aria-hidden="true" />
                  Đăng nhập
                </Link>
                <Link to="/register" className="btn-primary py-2">
                  <UserRoundPlus className="h-4 w-4" aria-hidden="true" />
                  Đăng ký
                </Link>
              </>
            )}
          </div>

          <button
            type="button"
            className="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-slate-200 text-slate-700 md:hidden"
            onClick={() => setIsOpen((value) => !value)}
            aria-label="Mở menu"
          >
            <Menu className="h-5 w-5" aria-hidden="true" />
          </button>
        </div>

        {isOpen ? (
          <div className="border-t border-slate-100 py-4 md:hidden">
            <div className="grid gap-2">
              {navItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={navLinkClass}
                  onClick={() => setIsOpen(false)}
                >
                  <item.icon className="h-4 w-4" aria-hidden="true" />
                  {item.label}
                </NavLink>
              ))}
              {session ? (
                <button
                  type="button"
                  onClick={handleLogout}
                  className="btn-secondary mt-2"
                >
                  <LogOut className="h-4 w-4" aria-hidden="true" />
                  Đăng xuất
                </button>
              ) : (
                <div className="mt-2 grid grid-cols-2 gap-2">
                  <Link to="/login" className="btn-secondary">
                    Đăng nhập
                  </Link>
                  <Link to="/register" className="btn-primary">
                    Đăng ký
                  </Link>
                </div>
              )}
            </div>
          </div>
        ) : null}
      </div>
    </header>
  )
}
