import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import type { UserRole } from '../types/auth'
import { getStoredAuth, hasAnyRole } from '../utils/authStorage'

interface ProtectedRouteProps {
  children: ReactNode
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({
  children,
  allowedRoles,
}: ProtectedRouteProps) {
  const location = useLocation()
  const session = getStoredAuth()

  if (!session?.accessToken) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (allowedRoles && !hasAnyRole(session, allowedRoles)) {
    return <Navigate to="/" replace />
  }

  return children
}
