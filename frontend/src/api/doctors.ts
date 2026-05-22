import { apiClient } from './client'
import type {
  CreateDoctorScheduleSlotRequest,
  Doctor,
  DoctorAppointment,
  DoctorAvatarUploadResponse,
  DoctorDashboardSummary,
  DoctorFilters,
  DoctorScheduleSlot,
  DoctorScheduleSlotActionResponse,
  DoctorSelfProfile,
  UpdateDoctorProfileRequest,
  UpdateDoctorProfileResponse,
  UpdateDoctorScheduleSlotRequest,
} from '../types/doctor'

function normalizeList<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[]
  }

  if (payload && typeof payload === 'object') {
    const shapedPayload = payload as {
      items?: T[]
      data?: T[]
      results?: T[]
    }

    return shapedPayload.items ?? shapedPayload.data ?? shapedPayload.results ?? []
  }

  return []
}

export async function getDoctors(filters: DoctorFilters): Promise<Doctor[]> {
  const response = await apiClient.get<unknown>('/api/doctors', {
    params: filters,
  })
  return normalizeList<Doctor>(response.data)
}

export async function getDoctorById(doctorProfileId: string): Promise<Doctor> {
  const response = await apiClient.get<Doctor>(`/api/doctors/${doctorProfileId}`)
  return response.data
}

export async function getDoctorAvailableSlots(
  doctorProfileId: string,
): Promise<DoctorScheduleSlot[]> {
  const response = await apiClient.get<unknown>(
    `/api/doctors/${doctorProfileId}/available-slots`,
  )
  return normalizeList<DoctorScheduleSlot>(response.data)
}

export async function getMyDoctorProfile(): Promise<DoctorSelfProfile> {
  const response = await apiClient.get<DoctorSelfProfile>('/api/doctors/me/profile')
  return response.data
}

export async function updateMyDoctorProfile(
  request: UpdateDoctorProfileRequest,
): Promise<UpdateDoctorProfileResponse> {
  const response = await apiClient.put<UpdateDoctorProfileResponse>(
    '/api/doctors/me/profile',
    request,
  )
  return response.data
}

export async function uploadMyDoctorAvatar(
  file: File,
): Promise<DoctorAvatarUploadResponse> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await apiClient.post<DoctorAvatarUploadResponse>(
    '/api/doctors/me/avatar',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
  )
  return response.data
}

export async function getMyDoctorDashboard(): Promise<DoctorDashboardSummary> {
  const response = await apiClient.get<DoctorDashboardSummary>(
    '/api/doctors/me/dashboard',
  )
  return response.data
}

export async function getMyDoctorAppointments(): Promise<DoctorAppointment[]> {
  const response = await apiClient.get<DoctorAppointment[]>(
    '/api/doctors/me/appointments',
  )
  return response.data
}

export async function getMyDoctorAppointment(
  appointmentId: string,
): Promise<DoctorAppointment> {
  const response = await apiClient.get<DoctorAppointment>(
    `/api/doctors/me/appointments/${appointmentId}`,
  )
  return response.data
}

export async function getDoctorScheduleSlots(
  doctorProfileId: string,
): Promise<DoctorScheduleSlot[]> {
  const response = await apiClient.get<unknown>(
    `/api/doctors/${doctorProfileId}/schedule-slots`,
  )
  return normalizeList<DoctorScheduleSlot>(response.data)
}

export async function createDoctorScheduleSlot(
  doctorProfileId: string,
  request: CreateDoctorScheduleSlotRequest,
): Promise<DoctorScheduleSlotActionResponse> {
  const response = await apiClient.post<DoctorScheduleSlotActionResponse>(
    `/api/doctors/${doctorProfileId}/schedule-slots`,
    request,
  )
  return response.data
}

export async function updateDoctorScheduleSlot(
  doctorProfileId: string,
  slotId: string,
  request: UpdateDoctorScheduleSlotRequest,
): Promise<DoctorScheduleSlotActionResponse> {
  const response = await apiClient.put<DoctorScheduleSlotActionResponse>(
    `/api/doctors/${doctorProfileId}/schedule-slots/${slotId}`,
    request,
  )
  return response.data
}

export async function disableDoctorScheduleSlot(
  doctorProfileId: string,
  slotId: string,
): Promise<DoctorScheduleSlotActionResponse> {
  const response = await apiClient.delete<DoctorScheduleSlotActionResponse>(
    `/api/doctors/${doctorProfileId}/schedule-slots/${slotId}`,
  )
  return response.data
}
