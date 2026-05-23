import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarClock, XCircle } from 'lucide-react'
import type { ReactNode } from 'react'
import {
  cancelAppointment,
  confirmAppointmentPayment,
  getPatientAppointments,
} from '../api/appointmentApi'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { getPatientProfileId } from '../utils/authStorage'
import { formatDateTime, shortId } from '../utils/formatters'

export function PatientAppointmentsPage() {
  const queryClient = useQueryClient()
  const patientProfileId = getPatientProfileId()
  const appointmentsQuery = useQuery({
    queryKey: ['patient-appointments', patientProfileId],
    queryFn: () => getPatientAppointments(patientProfileId!),
    enabled: Boolean(patientProfileId),
  })

  const cancelMutation = useMutation({
    mutationFn: ({
      appointmentId,
      cancellationReason,
    }: {
      appointmentId: string
      cancellationReason: string
    }) => cancelAppointment(appointmentId, { cancellationReason }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['patient-appointments', patientProfileId],
      })
    },
  })

  const confirmMutation = useMutation({
    mutationFn: confirmAppointmentPayment,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['patient-appointments', patientProfileId],
      })
    },
  })

  function handleCancel(appointmentId: string) {
    const cancellationReason = window.prompt(
      'Nhập lý do hủy lịch hẹn phục hồi sau đột quỵ:',
      'Patient requested appointment cancellation',
    )

    if (cancellationReason === null) {
      return
    }

    cancelMutation.mutate({
      appointmentId,
      cancellationReason:
        cancellationReason.trim() || 'Patient requested appointment cancellation',
    })
  }

  return (
    <DashboardLayout
      title="Lịch hẹn"
      description="Theo dõi lịch hẹn phục hồi, trạng thái giữ chỗ và trạng thái xác nhận."
    >
      {!patientProfileId ? (
        <ErrorState
          title="Thiếu Patient Profile"
          message="Hãy đăng xuất rồi đăng nhập lại để frontend nhận patientProfileId."
        />
      ) : null}

      {appointmentsQuery.isLoading ? <LoadingState /> : null}

      {appointmentsQuery.isError ? (
        <ErrorState message={getApiErrorMessage(appointmentsQuery.error)} />
      ) : null}

      {cancelMutation.isError ? (
        <div className="mb-5">
          <ErrorState message={getApiErrorMessage(cancelMutation.error)} />
        </div>
      ) : null}

      {confirmMutation.isError ? (
        <div className="mb-5">
          <ErrorState message={getApiErrorMessage(confirmMutation.error)} />
        </div>
      ) : null}

      {appointmentsQuery.isSuccess && appointmentsQuery.data.length === 0 ? (
        <EmptyState
          icon={CalendarClock}
          title="Chưa có lịch hẹn"
          message="Các lịch tư vấn phục hồi sau đột quỵ sẽ xuất hiện ở đây sau khi đặt lịch."
        />
      ) : null}

      {appointmentsQuery.isSuccess && appointmentsQuery.data.length > 0 ? (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <HeaderCell>Lịch hẹn</HeaderCell>
                  <HeaderCell>Bác sĩ</HeaderCell>
                  <HeaderCell>Dịch vụ</HeaderCell>
                  <HeaderCell>Thời gian</HeaderCell>
                  <HeaderCell>Trạng thái</HeaderCell>
                  <HeaderCell>Lý do</HeaderCell>
                  <HeaderCell>Thao tác</HeaderCell>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {appointmentsQuery.data.map((appointment) => {
                  const canCancel =
                    appointment.status === 'PendingPayment' ||
                    appointment.status === 'Confirmed'
                  const canConfirm = appointment.status === 'PendingPayment'

                  return (
                    <tr key={appointment.id}>
                      <BodyCell>#{shortId(appointment.id)}</BodyCell>
                      <BodyCell>#{shortId(appointment.doctorProfileId)}</BodyCell>
                      <BodyCell>#{shortId(appointment.medicalServiceId)}</BodyCell>
                      <BodyCell>
                        <div className="space-y-1">
                          <p>{formatDateTime(appointment.startTime)}</p>
                          <p className="text-xs text-slate-500">
                            đến {formatDateTime(appointment.endTime)}
                          </p>
                        </div>
                      </BodyCell>
                      <BodyCell>
                        <StatusBadge value={appointment.status} />
                      </BodyCell>
                      <BodyCell>
                        <div className="max-w-[260px] whitespace-normal">
                          <p>
                            {appointment.reason ?? 'Post-stroke rehabilitation'}
                          </p>
                          {appointment.status === 'Rejected' &&
                          appointment.cancellationReason ? (
                            <p className="mt-2 text-xs font-semibold text-red-700">
                              Rejection reason: {appointment.cancellationReason}
                            </p>
                          ) : null}
                        </div>
                      </BodyCell>
                      <BodyCell>
                        <div className="flex flex-wrap gap-2">
                          {canConfirm ? (
                            <button
                              type="button"
                              className="rounded-lg border border-care-200 bg-care-50 px-3 py-2 text-xs font-bold text-care-800 transition hover:border-care-400"
                              disabled={confirmMutation.isPending}
                              onClick={() => confirmMutation.mutate(appointment.id)}
                            >
                              Confirm Payment
                            </button>
                          ) : null}
                          {canCancel ? (
                            <button
                              type="button"
                              className="inline-flex items-center gap-1 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-bold text-red-700 transition hover:border-red-300"
                              disabled={cancelMutation.isPending}
                              onClick={() => handleCancel(appointment.id)}
                            >
                              <XCircle className="h-3.5 w-3.5" aria-hidden="true" />
                              Hủy
                            </button>
                          ) : null}
                        </div>
                      </BodyCell>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </DashboardLayout>
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
