import type { AuthSession, LoginResponse, UserRole } from '../types/auth'
import { decodeJwtPayload } from './jwt'

const AUTH_STORAGE_KEY = 'rehabai.auth'

function normalizeRoles(roles: string[]): UserRole[] {
  return roles.filter(Boolean) as UserRole[]
}

export function getStoredAuth(): AuthSession | null {
  if (typeof window === 'undefined') {
    return null
  }

  const rawValue = window.localStorage.getItem(AUTH_STORAGE_KEY)
  if (!rawValue) {
    return null
  }

  try {
    const parsed = JSON.parse(rawValue) as AuthSession
    if (!parsed.accessToken) {
      return null
    }

    const patientProfileId =
      parsed.patientProfileId ?? getPatientProfileIdFromToken(parsed.accessToken)
    const doctorProfileId =
      parsed.doctorProfileId ?? getDoctorProfileIdFromToken(parsed.accessToken)

    if (
      patientProfileId !== parsed.patientProfileId ||
      doctorProfileId !== parsed.doctorProfileId
    ) {
      const hydratedSession = { ...parsed, patientProfileId, doctorProfileId }
      window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(hydratedSession))
      return hydratedSession
    }

    return parsed
  } catch {
    window.localStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

export function storeAuth(response: LoginResponse): AuthSession {
  const patientProfileId =
    response.patientProfileId ?? getPatientProfileIdFromToken(response.accessToken)
  const doctorProfileId =
    response.doctorProfileId ?? getDoctorProfileIdFromToken(response.accessToken)
  const session: AuthSession = {
    accessToken: response.accessToken,
    userId: response.userId,
    email: response.email,
    fullName: response.fullName,
    roles: normalizeRoles(response.roles),
    patientProfileId,
    doctorProfileId,
  }

  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
  return session
}

export function getPatientProfileId(session = getStoredAuth()): string | null {
  return session?.patientProfileId ?? null
}

export function getDoctorProfileId(session = getStoredAuth()): string | null {
  return session?.doctorProfileId ?? null
}

export function updateStoredAuthProfile(update: Partial<Pick<AuthSession, 'fullName'>>): void {
  const session = getStoredAuth()

  if (!session) {
    return
  }

  window.localStorage.setItem(
    AUTH_STORAGE_KEY,
    JSON.stringify({ ...session, ...update }),
  )
}

export function clearAuth(): void {
  window.localStorage.removeItem(AUTH_STORAGE_KEY)
}

export function hasAnyRole(
  session: AuthSession | null,
  allowedRoles: UserRole[],
): boolean {
  if (!session) {
    return false
  }

  return session.roles.some((role) => allowedRoles.includes(role))
}

export function getDefaultRouteForRoles(roles: UserRole[]): string {
  if (roles.includes('Admin')) {
    return '/admin/dashboard'
  }

  if (roles.includes('Doctor')) {
    return '/doctor/dashboard'
  }

  if (roles.includes('Patient')) {
    return '/patient/dashboard'
  }

  return '/doctors'
}

function getPatientProfileIdFromToken(token: string): string | null {
  return decodeJwtPayload(token)?.patientProfileId ?? null
}

function getDoctorProfileIdFromToken(token: string): string | null {
  return decodeJwtPayload(token)?.doctorProfileId ?? null
}
