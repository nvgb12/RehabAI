import { apiClient } from './client'
import type {
  PatientProfile,
  UpdatePatientProfileRequest,
  UpdatePatientProfileResponse,
} from '../types/patient'

export async function getPatientProfile(
  patientProfileId: string,
): Promise<PatientProfile> {
  const response = await apiClient.get<PatientProfile>(
    `/api/patients/${patientProfileId}/profile`,
  )
  return response.data
}

export async function updatePatientProfile(
  patientProfileId: string,
  request: UpdatePatientProfileRequest,
): Promise<UpdatePatientProfileResponse> {
  const response = await apiClient.put<UpdatePatientProfileResponse>(
    `/api/patients/${patientProfileId}/profile`,
    request,
  )
  return response.data
}
