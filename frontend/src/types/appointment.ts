export interface Appointment {
  id: string
  patientProfileId: string
  doctorProfileId: string
  medicalServiceId: string
  scheduleSlotId: string
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

export interface CancelAppointmentRequest {
  cancellationReason?: string | null
}

export interface AppointmentActionResponse {
  message: string
  appointment: Appointment
}
