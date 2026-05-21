import { Outlet } from 'react-router-dom'
import { AppFooter } from '../components/AppFooter'
import { AppHeader } from '../components/AppHeader'

export function MainLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-white">
      <AppHeader />
      <main className="flex-1">
        <Outlet />
      </main>
      <AppFooter />
    </div>
  )
}
