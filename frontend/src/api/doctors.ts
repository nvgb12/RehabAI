import { apiClient } from './client'
import type { Doctor, DoctorFilters, DoctorScheduleSlot } from '../types/doctor'

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
