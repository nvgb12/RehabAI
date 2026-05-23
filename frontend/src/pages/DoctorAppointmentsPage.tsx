import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarClock, CheckCircle2, Eye, XCircle } from 'lucide-react'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  acceptMyDoctorAppointmentRequest,
  getMyDoctorAppointmentRequests,
  getMyDoctorAppointments,
  rejectMyDoctorAppointmentRequest,
} from '../api/doctors'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import type { DoctorAppointment } from '../types/doctor'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDateTime, shortId } from '../utils/formatters'

type AppointmentTab = 'appointments' | 'requests'

export function DoctorAppointmentsPage() {
  const queryClient = useQueryClient()
  const [activeTab, setActiveTab] = useState<AppointmentTab>('appointments')
  const [rejectingAppointment, setRejectingAppointment] =
    useState<DoctorAppointment | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [rejectionError, setRejectionError] = useState<string | null>(null)

  const appointmentsQuery = useQuery({
    queryKey: ['doctor-appointments'],
    queryFn: getMyDoctorAppointments,
    enabled: activeTab === 'appointments',
  })

  const requestsQuery = useQuery({
    queryKey: ['doctor-appointment-requests'],
    queryFn: getMyDoctorAppointmentRequests,
    enabled: activeTab === 'requests',
  })

  const acceptMutation = useMutation({
    mutationFn: acceptMyDoctorAppointmentRequest,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['doctor-appointment-requests'],
        }),
        queryClient.invalidateQueries({ queryKey: ['doctor-appointments'] }),
      ])
    },
  })

  const rejectMutation = useMutation({
    mutationFn: ({
      appointmentId,
      rejectionReason,
    }: {
      appointmentId: string
      rejectionReason: string
    }) =>
      rejectMyDoctorAppointmentRequest(appointmentId, {
        rejectionReason,
      }),
    onSuccess: async () => {
      setRejectingAppointment(null)
      setRejectionReason('')
      setRejectionError(null)
      await queryClient.invalidateQueries({
        queryKey: ['doctor-appointment-requests'],
      })
    },
  })

  function openRejectDialog(appointment: DoctorAppointment) {
    setRejectingAppointment(appointment)
    setRejectionReason('')
    setRejectionError(null)
  }

  function submitReject() {
    const normalizedReason = rejectionReason.trim()

    if (!rejectingAppointment) {
      return
    }

    if (!normalizedReason) {
      setRejectionError('Rejection reason is required.')
      return
    }

    rejectMutation.mutate({
      appointmentId: rejectingAppointment.appointmentId,
      rejectionReason: normalizedReason,
    })
  }

  return (
    <DoctorLayout
      title="Doctor Appointments"
      description="Review direct appointments and flexible appointment requests assigned to your Doctor profile."
    >
      <div className="mb-5 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          className={tabClassName(activeTab === 'appointments')}
          onClick={() => setActiveTab('appointments')}
        >
          Appointments
        </button>
        <button
          type="button"
          className={tabClassName(activeTab === 'requests')}
          onClick={() => setActiveTab('requests')}
        >
          Requests
        </button>
      </div>

      {activeTab === 'appointments' ? (
        <AppointmentsTable
          appointments={appointmentsQuery.data ?? []}
          isLoading={appointmentsQuery.isLoading}
          error={appointmentsQuery.isError ? appointmentsQuery.error : null}
        />
      ) : (
        <AppointmentRequestsTable
          requests={requestsQuery.data ?? []}
          isLoading={requestsQuery.isLoading}
          error={requestsQuery.isError ? requestsQuery.error : null}
          isAccepting={acceptMutation.isPending}
          isRejecting={rejectMutation.isPending}
          actionError={
            acceptMutation.isError
              ? getApiErrorMessage(acceptMutation.error)
              : rejectMutation.isError
                ? getApiErrorMessage(rejectMutation.error)
                : null
          }
          onAccept={(appointmentId) =>
            acceptMutation.mutate(appointmentId)
          }
          onReject={openRejectDialog}
        />
      )}

      {rejectingAppointment ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/40 p-4">
          <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-xl">
            <h2 className="text-xl font-bold text-slate-950">
              Reject appointment request
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              Appointment #{shortId(rejectingAppointment.appointmentId)} for{' '}
              {rejectingAppointment.patientName}
            </p>
            <label className="mt-5 block">
              <span className="field-label">Rejection reason</span>
              <textarea
                className="field-input mt-2 min-h-28 resize-y"
                value={rejectionReason}
                onChange={(event) => {
                  setRejectionReason(event.target.value)
                  setRejectionError(null)
                }}
                placeholder="Explain why this request cannot be accepted."
              />
            </label>
            {rejectionError ? (
              <p className="mt-2 text-sm font-semibold text-red-600">
                {rejectionError}
              </p>
            ) : null}
            {rejectMutation.isError ? (
              <div className="mt-4">
                <ErrorState message={getApiErrorMessage(rejectMutation.error)} />
              </div>
            ) : null}
            <div className="mt-6 flex flex-col gap-3 sm:flex-row">
              <button
                type="button"
                className="inline-flex items-center justify-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm font-bold text-red-700 transition hover:border-red-300"
                disabled={rejectMutation.isPending}
                onClick={submitReject}
              >
                <XCircle className="h-4 w-4" aria-hidden="true" />
                {rejectMutation.isPending ? 'Rejecting' : 'Reject'}
              </button>
              <button
                type="button"
                className="btn-secondary"
                disabled={rejectMutation.isPending}
                onClick={() => {
                  setRejectingAppointment(null)
                  setRejectionReason('')
                  setRejectionError(null)
                }}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </DoctorLayout>
  )
}

function AppointmentsTable({
  appointments,
  isLoading,
  error,
}: {
  appointments: DoctorAppointment[]
  isLoading: boolean
  error: unknown
}) {
  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={getApiErrorMessage(error)} />
  }

  if (appointments.length === 0) {
    return (
      <EmptyState
        icon={CalendarClock}
        title="No appointments yet"
        message="Patient bookings assigned to your profile will appear here."
      />
    )
  }

  return (
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
            {appointments.map((appointment) => (
              <tr key={appointment.appointmentId}>
                <BodyCell>#{shortId(appointment.appointmentId)}</BodyCell>
                <BodyCell>{appointment.patientName}</BodyCell>
                <BodyCell>{appointment.medicalServiceName}</BodyCell>
                <BodyCell>
                  <TimeRange appointment={appointment} />
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
                  <ReasonText appointment={appointment} />
                </BodyCell>
                <BodyCell>
                  <DetailLink appointmentId={appointment.appointmentId} />
                </BodyCell>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

function AppointmentRequestsTable({
  requests,
  isLoading,
  error,
  isAccepting,
  isRejecting,
  actionError,
  onAccept,
  onReject,
}: {
  requests: DoctorAppointment[]
  isLoading: boolean
  error: unknown
  isAccepting: boolean
  isRejecting: boolean
  actionError: string | null
  onAccept: (appointmentId: string) => void
  onReject: (appointment: DoctorAppointment) => void
}) {
  if (isLoading) {
    return <LoadingState />
  }

  if (error) {
    return <ErrorState message={getApiErrorMessage(error)} />
  }

  return (
    <div className="grid gap-5">
      {actionError ? <ErrorState message={actionError} /> : null}

      {requests.length === 0 ? (
        <EmptyState
          icon={CalendarClock}
          title="No appointment requests"
          message="Flexible Patient requests waiting for your review will appear here."
        />
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="min-w-[1120px] divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <HeaderCell>Request</HeaderCell>
                  <HeaderCell>Patient</HeaderCell>
                  <HeaderCell>Service</HeaderCell>
                  <HeaderCell>Preferred time</HeaderCell>
                  <HeaderCell>Reason</HeaderCell>
                  <HeaderCell>Created</HeaderCell>
                  <HeaderCell>Status</HeaderCell>
                  <HeaderCell>Actions</HeaderCell>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {requests.map((appointment) => (
                  <tr key={appointment.appointmentId}>
                    <BodyCell>#{shortId(appointment.appointmentId)}</BodyCell>
                    <BodyCell>{appointment.patientName}</BodyCell>
                    <BodyCell>{appointment.medicalServiceName}</BodyCell>
                    <BodyCell>
                      <TimeRange appointment={appointment} />
                    </BodyCell>
                    <BodyCell>
                      <ReasonText appointment={appointment} />
                    </BodyCell>
                    <BodyCell>{formatDateTime(appointment.createdAt)}</BodyCell>
                    <BodyCell>
                      <StatusBadge value={appointment.status} />
                    </BodyCell>
                    <BodyCell>
                      <div className="flex flex-wrap gap-2">
                        <button
                          type="button"
                          className="inline-flex items-center gap-1 rounded-lg border border-rehab-200 bg-rehab-50 px-3 py-2 text-xs font-bold text-rehab-800 transition hover:border-rehab-300"
                          disabled={isAccepting || isRejecting}
                          onClick={() => onAccept(appointment.appointmentId)}
                        >
                          <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" />
                          Accept
                        </button>
                        <button
                          type="button"
                          className="inline-flex items-center gap-1 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-bold text-red-700 transition hover:border-red-300"
                          disabled={isAccepting || isRejecting}
                          onClick={() => onReject(appointment)}
                        >
                          <XCircle className="h-3.5 w-3.5" aria-hidden="true" />
                          Reject
                        </button>
                      </div>
                    </BodyCell>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function TimeRange({ appointment }: { appointment: DoctorAppointment }) {
  return (
    <div className="space-y-1">
      <p>{formatDateTime(appointment.startTime)}</p>
      <p className="text-xs text-slate-500">
        to {formatDateTime(appointment.endTime)}
      </p>
    </div>
  )
}

function ReasonText({ appointment }: { appointment: DoctorAppointment }) {
  return (
    <span className="line-clamp-2 max-w-[260px] whitespace-normal text-sm">
      {appointment.notes ?? 'Post-stroke rehabilitation'}
    </span>
  )
}

function DetailLink({ appointmentId }: { appointmentId: string }) {
  return (
    <Link
      to={`/doctor/appointments/${appointmentId}`}
      className="inline-flex items-center gap-2 rounded-lg border border-care-200 bg-care-50 px-3 py-2 text-xs font-bold text-care-800 transition hover:border-care-400"
    >
      <Eye className="h-3.5 w-3.5" aria-hidden="true" />
      View
    </Link>
  )
}

function tabClassName(isActive: boolean): string {
  return [
    'rounded-md px-4 py-2 text-sm font-bold transition',
    isActive
      ? 'bg-care-800 text-white'
      : 'text-slate-600 hover:bg-slate-50 hover:text-care-800',
  ].join(' ')
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
