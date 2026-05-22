import { apiClient } from './client'
import type {
  LoginRequest,
  LoginResponse,
  RegisterPatientRequest,
  RegisterPatientResponse,
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
