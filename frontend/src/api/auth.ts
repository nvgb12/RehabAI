import { apiClient } from './client'
import type {
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  LoginResponse,
  RegisterPatientRequest,
  RegisterPatientResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  SetupDoctorPasswordRequest,
  SetupDoctorPasswordResponse,
} from '../types/auth'

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/api/Auth/login', request)
  return response.data
}

export async function registerPatient(
  request: RegisterPatientRequest,
): Promise<RegisterPatientResponse> {
  const response = await apiClient.post<RegisterPatientResponse>(
    '/api/Auth/register-patient',
    request,
  )
  return response.data
}

export async function setupDoctorPassword(
  request: SetupDoctorPasswordRequest,
): Promise<SetupDoctorPasswordResponse> {
  const response = await apiClient.post<SetupDoctorPasswordResponse>(
    '/api/Auth/setup-doctor-password',
    request,
  )
  return response.data
}

export async function forgotPassword(
  request: ForgotPasswordRequest,
): Promise<ForgotPasswordResponse> {
  const response = await apiClient.post<ForgotPasswordResponse>(
    '/api/Auth/forgot-password',
    request,
  )
  return response.data
}

export async function resetPassword(
  request: ResetPasswordRequest,
): Promise<ResetPasswordResponse> {
  const response = await apiClient.post<ResetPasswordResponse>(
    '/api/Auth/reset-password',
    request,
  )
  return response.data
}
