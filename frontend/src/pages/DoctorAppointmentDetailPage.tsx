import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarClock,
  ClipboardList,
  Stethoscope,
  UserRound,
} from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { getMyDoctorAppointment } from '../api/doctors'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDateTime, shortId } from '../utils/formatters'

export function DoctorAppointmentDetailPage() {
  const { appointmentId } = useParams<{ appointmentId: string }>()

  const appointmentQuery = useQuery({
    queryKey: ['doctor-appointment', appointmentId],
    queryFn: () => getMyDoctorAppointment(appointmentId!),
    enabled: Boolean(appointmentId),
  })

  const appointment = appointmentQuery.data

  return (
    <DoctorLayout
      title="Appointment Detail"
      description="Doctor read-only view for an assigned stroke rehabilitation appointment."
    >
      <Link to="/doctor/appointments" className="btn-secondary mb-5 px-4 py-2">
        <ArrowLeft className="h-4 w-4" aria-hidden="true" />
        Back to appointments
      </Link>

      {!appointmentId ? (
        <ErrorState title="Missing appointment" message="Appointment id is required." />
      ) : null}

      {appointmentQuery.isLoading ? <LoadingState /> : null}

      {appointmentQuery.isError ? (
        <ErrorState message={getApiErrorMessage(appointmentQuery.error)} />
      ) : null}

      {appointment ? (
        <div className="grid gap-5 lg:grid-cols-[1fr_1.1fr]">
          <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <p className="text-xs font-bold uppercase text-slate-500">
                  Appointment
                </p>
                <h2 className="mt-2 text-2xl font-bold text-slate-950">
                  #{shortId(appointment.appointmentId)}
                </h2>
              </div>
              <div className="flex flex-wrap gap-2">
                <StatusBadge value={appointment.status} />
                {appointment.paymentStatus ? (
                  <StatusBadge value={appointment.paymentStatus} />
                ) : null}
              </div>
            </div>

            <div className="mt-6 grid gap-4">
              <InfoRow
                icon={UserRound}
                label="Patient"
                value={appointment.patientName}
              />
              <InfoRow
                icon={Stethoscope}
                label="Service"
                value={appointment.medicalServiceName}
              />
              <InfoRow
                icon={CalendarClock}
                label="Start"
                value={formatDateTime(appointment.startTime)}
              />
              <InfoRow
                icon={CalendarClock}
                label="End"
                value={formatDateTime(appointment.endTime)}
              />
            </div>
          </section>

          <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-xl font-bold text-slate-950">
              Rehabilitation reason / notes
            </h2>
            <p className="mt-4 whitespace-pre-wrap rounded-lg bg-slate-50 p-4 text-sm leading-6 text-slate-700">
              {appointment.notes ??
                appointment.reason ??
                'No appointment reason was provided.'}
            </p>

            <div className="mt-5 grid gap-4 sm:grid-cols-2">
              <InfoBlock label="Patient profile" value={appointment.patientProfileId} />
              <InfoBlock
                label="Schedule slot"
                value={appointment.doctorScheduleSlotId}
              />
              <InfoBlock
                label="Medical service"
                value={appointment.medicalServiceId}
              />
              <InfoBlock label="Created" value={formatDateTime(appointment.createdAt)} />
            </div>
          </section>
        </div>
      ) : null}
    </DoctorLayout>
  )
}

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof ClipboardList
  label: string
  value: string
}) {
  return (
    <div className="flex items-start gap-3 rounded-lg bg-slate-50 p-4">
      <Icon className="mt-0.5 h-5 w-5 text-care-700" aria-hidden="true" />
      <div>
        <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
        <p className="mt-1 text-sm font-bold text-slate-950">{value}</p>
      </div>
    </div>
  )
}

function InfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
      <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
      <p className="mt-2 break-words text-sm font-bold text-slate-950">
        {value}
      </p>
    </div>
  )
}
