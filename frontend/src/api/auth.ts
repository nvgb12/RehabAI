import { apiClient } from './client'
import type {
  LoginRequest,
  LoginResponse,
  RegisterPatientRequest,
  RegisterPatientResponse,
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
