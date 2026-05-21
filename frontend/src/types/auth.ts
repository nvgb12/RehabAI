export type UserRole =
  | 'Patient'
  | 'Doctor'
  | 'Admin'
  | 'AuthorizedInternalStaff'
  | 'VerificationAdmin'
  | 'SupportStaff'
  | 'FinanceAdmin'

export interface AuthSession {
  accessToken: string
  userId: string
  email: string
  fullName: string
  roles: UserRole[]
  patientProfileId?: string | null
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  message: string
  userId: string
  email: string
  fullName: string
  roles: string[]
  accessToken: string
  patientProfileId?: string | null
}

export interface RegisterPatientRequest {
  fullName: string
  email: string
  phoneNumber?: string
  password: string
}

export interface RegisterPatientResponse {
  message: string
  userId?: string
  email?: string
  verificationToken?: string
  verificationUrl?: string
}
