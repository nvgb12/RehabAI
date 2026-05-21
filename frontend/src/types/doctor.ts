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
