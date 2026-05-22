export interface JwtPayload {
  sub?: string
  email?: string
  name?: string
  roles?: string[] | string
  patientProfileId?: string
  doctorProfileId?: string
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const [, payload] = token.split('.')
  if (!payload || typeof globalThis.atob !== 'function') {
    return null
  }

  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const paddedBase64 = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
    const decoded = globalThis.atob(paddedBase64)
    const json = decodeURIComponent(
      Array.from(decoded, (character) =>
        `%${character.charCodeAt(0).toString(16).padStart(2, '0')}`,
      ).join(''),
    )

    return JSON.parse(json) as JwtPayload
  } catch {
    return null
  }
}
