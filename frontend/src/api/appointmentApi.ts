import { apiClient } from './client'
import type {
  Appointment,
  AppointmentActionResponse,
  CancelAppointmentRequest,
  CreateFlexibleAppointmentRequest,
  CreateAppointmentRequest,
} from '../types/appointment'

export async function createAppointment(
  request: CreateAppointmentRequest,
): Promise<AppointmentActionResponse> {
  const response = await apiClient.post<AppointmentActionResponse>(
    '/api/appointments',
    request,
  )
  return response.data
}

export async function createAppointmentRequest(
  request: CreateFlexibleAppointmentRequest,
): Promise<AppointmentActionResponse> {
  const response = await apiClient.post<AppointmentActionResponse>(
    '/api/appointments/requests',
    request,
  )
  return response.data
}

export async function confirmAppointmentPayment(
  appointmentId: string,
): Promise<AppointmentActionResponse> {
  const response = await apiClient.post<AppointmentActionResponse>(
    `/api/appointments/${appointmentId}/confirm-payment`,
  )
  return response.data
}

export async function cancelAppointment(
  appointmentId: string,
  request: CancelAppointmentRequest,
): Promise<AppointmentActionResponse> {
  const response = await apiClient.post<AppointmentActionResponse>(
    `/api/appointments/${appointmentId}/cancel`,
    request,
  )
  return response.data
}

export async function getPatientAppointments(
  patientProfileId: string,
): Promise<Appointment[]> {
  const response = await apiClient.get<Appointment[]>(
    `/api/patients/${patientProfileId}/appointments`,
  )
  return response.data
}
