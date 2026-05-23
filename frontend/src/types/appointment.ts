export interface Appointment {
  id: string
  patientProfileId: string
  doctorProfileId: string
  medicalServiceId: string
  scheduleSlotId?: string | null
  status: string
  startTime: string
  endTime: string
  reservedUntil?: string | null
  reason?: string | null
  cancellationReason?: string | null
}

export interface CreateAppointmentRequest {
  patientProfileId: string
  doctorProfileId: string
  medicalServiceId: string
  scheduleSlotId: string
  reason?: string | null
}

export interface CreateFlexibleAppointmentRequest {
  doctorProfileId: string
  medicalServiceId: string
  preferredStartTime: string
  preferredEndTime: string
  reason: string
}

export interface CancelAppointmentRequest {
  cancellationReason?: string | null
}

export interface AppointmentActionResponse {
  message: string
  appointment: Appointment
}
