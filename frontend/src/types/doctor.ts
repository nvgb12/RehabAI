export interface Doctor {
  doctorProfileId: string
  userId: string
  fullName: string
  specialtyId: string
  specialtyName: string
  bio?: string | null
  avatarUrl?: string | null
  nextAvailableSlotStartTime?: string | null
  nextAvailableSlotEndTime?: string | null
}

export interface DoctorFilters {
  keyword?: string
  specialtyId?: string
  availableFrom?: string
  availableTo?: string
}

export interface DoctorScheduleSlot {
  id: string
  doctorProfileId: string
  startTime: string
  endTime: string
  status: string
  reservedUntil?: string | null
  createdByUserId?: string | null
  updatedByUserId?: string | null
}

export interface DoctorSelfProfile {
  doctorProfileId: string
  userId: string
  fullName: string
  email: string
  phoneNumber?: string | null
  status: string
  emailConfirmed: boolean
  specialtyId: string
  specialtyName: string
  bio?: string | null
  yearsOfExperience?: number | null
  publicProfileApproved: boolean
  publicProfileReviewStatus: string
  submittedForReviewAt?: string | null
  reviewedAt?: string | null
  reviewedByAdminId?: string | null
  publicProfileRejectionReason?: string | null
  avatarUrl?: string | null
  profileImageUrl?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface UpdateDoctorProfileRequest {
  phoneNumber?: string | null
  bio?: string | null
  yearsOfExperience?: number | null
}

export interface UpdateDoctorProfileResponse {
  message: string
  profile: DoctorSelfProfile
}

export interface DoctorAppointment {
  appointmentId: string
  patientProfileId: string
  patientName: string
  medicalServiceId: string
  medicalServiceName: string
  doctorScheduleSlotId?: string | null
  startTime: string
  endTime: string
  status: string
  paymentStatus?: string | null
  notes?: string | null
  reason?: string | null
  createdAt: string
}

export interface DoctorNextAppointment {
  appointmentId: string
  patientName: string
  medicalServiceName: string
  startTime: string
  status: string
}

export interface DoctorDashboardSummary {
  doctorProfileId: string
  fullName: string
  publicProfileApproved: boolean
  upcomingAppointmentCount: number
  todayAppointmentCount: number
  availableSlotCount: number
  bookedSlotCount: number
  nextAppointment?: DoctorNextAppointment | null
}

export interface CreateDoctorScheduleSlotRequest {
  startTime: string
  endTime: string
}

export interface UpdateDoctorScheduleSlotRequest {
  startTime: string
  endTime: string
  status: string
}

export interface DoctorScheduleSlotActionResponse {
  message: string
  slot: DoctorScheduleSlot
}

export interface DoctorAvatarUploadResponse {
  avatarUrl: string
}

export interface DoctorPublicProfileSubmitResponse {
  message: string
  profile: DoctorSelfProfile
}

export interface DoctorAppointmentActionResponse {
  message: string
  appointment: DoctorAppointment
}

export interface RejectDoctorAppointmentRequest {
  rejectionReason: string
}
