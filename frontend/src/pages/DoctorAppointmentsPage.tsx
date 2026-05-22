import { useQuery } from '@tanstack/react-query'
import { CalendarClock, Eye } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { getMyDoctorAppointments } from '../api/doctors'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDateTime, shortId } from '../utils/formatters'

export function DoctorAppointmentsPage() {
  const appointmentsQuery = useQuery({
    queryKey: ['doctor-appointments'],
    queryFn: getMyDoctorAppointments,
  })

  return (
    <DoctorLayout
      title="Doctor Appointments"
      description="Read-only list of appointments assigned to your Doctor profile."
    >
      {appointmentsQuery.isLoading ? <LoadingState /> : null}

      {appointmentsQuery.isError ? (
        <ErrorState message={getApiErrorMessage(appointmentsQuery.error)} />
      ) : null}

      {appointmentsQuery.isSuccess && appointmentsQuery.data.length === 0 ? (
        <EmptyState
          icon={CalendarClock}
          title="No appointments yet"
          message="Patient bookings assigned to your profile will appear here."
        />
      ) : null}

      {appointmentsQuery.isSuccess && appointmentsQuery.data.length > 0 ? (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="min-w-[980px] divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <HeaderCell>Appointment</HeaderCell>
                  <HeaderCell>Patient</HeaderCell>
                  <HeaderCell>Service</HeaderCell>
                  <HeaderCell>Time</HeaderCell>
                  <HeaderCell>Status</HeaderCell>
                  <HeaderCell>Payment</HeaderCell>
                  <HeaderCell>Reason</HeaderCell>
                  <HeaderCell>Detail</HeaderCell>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {appointmentsQuery.data.map((appointment) => (
                  <tr key={appointment.appointmentId}>
                    <BodyCell>#{shortId(appointment.appointmentId)}</BodyCell>
                    <BodyCell>{appointment.patientName}</BodyCell>
                    <BodyCell>{appointment.medicalServiceName}</BodyCell>
                    <BodyCell>
                      <div className="space-y-1">
                        <p>{formatDateTime(appointment.startTime)}</p>
                        <p className="text-xs text-slate-500">
                          to {formatDateTime(appointment.endTime)}
                        </p>
                      </div>
                    </BodyCell>
                    <BodyCell>
                      <StatusBadge value={appointment.status} />
                    </BodyCell>
                    <BodyCell>
                      {appointment.paymentStatus ? (
                        <StatusBadge value={appointment.paymentStatus} />
                      ) : (
                        <span className="text-xs text-slate-500">N/A</span>
                      )}
                    </BodyCell>
                    <BodyCell>
                      <span className="line-clamp-2 max-w-[240px] whitespace-normal text-sm">
                        {appointment.notes ??
                          appointment.reason ??
                          'Post-stroke rehabilitation'}
                      </span>
                    </BodyCell>
                    <BodyCell>
                      <Link
                        to={`/doctor/appointments/${appointment.appointmentId}`}
                        className="inline-flex items-center gap-2 rounded-lg border border-care-200 bg-care-50 px-3 py-2 text-xs font-bold text-care-800 transition hover:border-care-400"
                      >
                        <Eye className="h-3.5 w-3.5" aria-hidden="true" />
                        View
                      </Link>
                    </BodyCell>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </DoctorLayout>
  )
}

function HeaderCell({ children }: { children: ReactNode }) {
  return (
    <th className="whitespace-nowrap px-5 py-4 text-left text-xs font-bold uppercase text-slate-500">
      {children}
    </th>
  )
}

function BodyCell({ children }: { children: ReactNode }) {
  return (
    <td className="whitespace-nowrap px-5 py-4 text-sm font-medium text-slate-700">
      {children}
    </td>
  )
}
