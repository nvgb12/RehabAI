import { apiClient } from './client'
import type { MedicalService } from '../types/medicalService'

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

export async function getMedicalServices(): Promise<MedicalService[]> {
  const response = await apiClient.get<unknown>('/api/medical-services')
  return normalizeList<MedicalService>(response.data)
}
