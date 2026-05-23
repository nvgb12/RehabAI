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
  doctorProfileId?: string | null
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
  doctorProfileId?: string | null
}

export interface SetupDoctorPasswordRequest {
  email: string
  token: string
  password: string
}

export interface SetupDoctorPasswordResponse {
  message: string
  email: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ForgotPasswordResponse {
  message: string
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}

export interface ResetPasswordResponse {
  message: string
  email: string
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
