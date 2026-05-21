export interface PatientProfile {
  patientProfileId: string
  userId: string
  fullName: string
  email: string
  phoneNumber?: string | null
  dateOfBirth?: string | null
  gender?: string | null
  address?: string | null
}

export interface UpdatePatientProfileRequest {
  fullName: string
  phoneNumber?: string | null
  dateOfBirth?: string | null
  gender?: string | null
  address?: string | null
}

export interface UpdatePatientProfileResponse {
  message: string
  profile: PatientProfile
}
