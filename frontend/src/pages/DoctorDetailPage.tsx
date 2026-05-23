import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  CalendarCheck,
  CalendarPlus,
  CreditCard,
  Stethoscope,
} from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import {
  confirmAppointmentPayment,
  createAppointment,
  createAppointmentRequest,
} from '../api/appointmentApi'
import {
  getDoctorAvailableSlots,
  getDoctorById,
} from '../api/doctors'
import { getMedicalServices } from '../api/medicalServices'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import type { Appointment } from '../types/appointment'
import { getApiErrorMessage } from '../utils/apiError'
import { getPatientProfileId, getStoredAuth } from '../utils/authStorage'
import {
  formatCurrency,
  formatDateTime,
  shortId,
} from '../utils/formatters'

const bookingSchema = z.object({
  medicalServiceId: z.string().min(1, 'Vui lòng chọn dịch vụ phục hồi.'),
  scheduleSlotId: z.string().min(1, 'Vui lòng chọn lịch trống.'),
  reason: z.string().trim().optional(),
})

type BookingFormValues = z.infer<typeof bookingSchema>

const requestSchema = z
  .object({
    medicalServiceId: z.string().min(1, 'Service is required.'),
    preferredStartTime: z.string().min(1, 'Preferred start time is required.'),
    preferredEndTime: z.string().min(1, 'Preferred end time is required.'),
    reason: z.string().trim().min(1, 'Reason is required.'),
  })
  .refine(
    (values) =>
      values.preferredStartTime.length > 0 &&
      values.preferredEndTime.length > 0 &&
      new Date(values.preferredStartTime) < new Date(values.preferredEndTime),
    {
      message: 'Preferred start time must be before preferred end time.',
      path: ['preferredEndTime'],
    },
  )

type RequestFormValues = z.infer<typeof requestSchema>

const reasonExamples = [
  'Post-stroke rehabilitation consultation',
  'Stroke mobility assessment',
  'Neurological rehabilitation follow-up',
  'Stroke recovery therapy session',
]

export function DoctorDetailPage() {
  const { doctorProfileId = '' } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const session = getStoredAuth()
  const patientProfileId = getPatientProfileId(session)
  const isPatient = Boolean(session?.roles.includes('Patient'))
  const [isBookingOpen, setIsBookingOpen] = useState(false)
  const [createdAppointment, setCreatedAppointment] =
    useState<Appointment | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [requestedAppointment, setRequestedAppointment] =
    useState<Appointment | null>(null)
  const [requestSuccessMessage, setRequestSuccessMessage] = useState<string | null>(
    null,
  )

  const doctorQuery = useQuery({
    queryKey: ['doctor', doctorProfileId],
    queryFn: () => getDoctorById(doctorProfileId),
    enabled: Boolean(doctorProfileId),
  })

  const slotsQuery = useQuery({
    queryKey: ['doctor-available-slots', doctorProfileId],
    queryFn: () => getDoctorAvailableSlots(doctorProfileId),
    enabled: Boolean(doctorProfileId),
  })

  const servicesQuery = useQuery({
    queryKey: ['medical-services'],
    queryFn: getMedicalServices,
  })

  const availableSlots = useMemo(() => slotsQuery.data ?? [], [slotsQuery.data])
  const hasAvailableSlots = availableSlots.length > 0

  const {
    register,
    handleSubmit,
    setValue,
    getValues,
    control,
    formState: { errors },
  } = useForm<BookingFormValues>({
    resolver: zodResolver(bookingSchema),
    defaultValues: {
      medicalServiceId: '',
      scheduleSlotId: '',
      reason: reasonExamples[0],
    },
  })

  const {
    register: registerRequest,
    handleSubmit: handleRequestSubmit,
    setValue: setRequestValue,
    getValues: getRequestValues,
    formState: { errors: requestErrors },
  } = useForm<RequestFormValues>({
    resolver: zodResolver(requestSchema),
    defaultValues: {
      medicalServiceId: '',
      preferredStartTime: '',
      preferredEndTime: '',
      reason: reasonExamples[0],
    },
  })

  useEffect(() => {
    if (servicesQuery.data?.length && !getValues('medicalServiceId')) {
      setValue('medicalServiceId', servicesQuery.data[0].id)
    }

    if (servicesQuery.data?.length && !getRequestValues('medicalServiceId')) {
      setRequestValue('medicalServiceId', servicesQuery.data[0].id)
    }
  }, [getRequestValues, getValues, servicesQuery.data, setRequestValue, setValue])

  useEffect(() => {
    const selectedSlotId = getValues('scheduleSlotId')

    if (
      availableSlots.length &&
      (!selectedSlotId || !availableSlots.some((slot) => slot.id === selectedSlotId))
    ) {
      setValue('scheduleSlotId', availableSlots[0].id)
    }
  }, [availableSlots, getValues, setValue])

  useEffect(() => {
    if (slotsQuery.isSuccess && availableSlots.length === 0) {
      setValue('scheduleSlotId', '')
    }
  }, [availableSlots.length, setValue, slotsQuery.isSuccess])

  const createMutation = useMutation({
    mutationFn: (values: BookingFormValues) =>
      createAppointment({
        patientProfileId: patientProfileId!,
        doctorProfileId,
        medicalServiceId: values.medicalServiceId,
        scheduleSlotId: values.scheduleSlotId,
        reason: normalizeReason(values.reason),
      }),
    onSuccess: async (response) => {
      setCreatedAppointment(response.appointment)
      setSuccessMessage(response.message)
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['doctor-available-slots', doctorProfileId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['patient-appointments', patientProfileId],
        }),
      ])
    },
  })

  const confirmMutation = useMutation({
    mutationFn: (appointmentId: string) => confirmAppointmentPayment(appointmentId),
    onSuccess: async (response) => {
      setCreatedAppointment(response.appointment)
      setSuccessMessage(response.message)
      await queryClient.invalidateQueries({
        queryKey: ['patient-appointments', patientProfileId],
      })
    },
  })

  const requestMutation = useMutation({
    mutationFn: (values: RequestFormValues) =>
      createAppointmentRequest({
        doctorProfileId,
        medicalServiceId: values.medicalServiceId,
        preferredStartTime: toIsoDateTime(values.preferredStartTime),
        preferredEndTime: toIsoDateTime(values.preferredEndTime),
        reason: values.reason.trim(),
      }),
    onSuccess: async (response) => {
      setRequestedAppointment(response.appointment)
      setRequestSuccessMessage(response.message)
      await queryClient.invalidateQueries({
        queryKey: ['patient-appointments', patientProfileId],
      })
    },
  })

  const selectedServiceId = useWatch({ control, name: 'medicalServiceId' })
  const selectedSlotId = useWatch({ control, name: 'scheduleSlotId' })
  const selectedService = servicesQuery.data?.find(
    (service) => service.id === selectedServiceId,
  )
  const selectedSlot = availableSlots.find((slot) => slot.id === selectedSlotId)
  const canConfirmPayment = createdAppointment?.status === 'PendingPayment'

  function openBooking() {
    setSuccessMessage(null)

    if (!session?.accessToken) {
      navigate('/login', { state: { from: location } })
      return
    }

    setIsBookingOpen(true)
  }

  function onSubmit(values: BookingFormValues) {
    setSuccessMessage(null)
    setCreatedAppointment(null)

    if (!patientProfileId || !hasAvailableSlots) {
      return
    }

    createMutation.mutate(values)
  }

  function onRequestSubmit(values: RequestFormValues) {
    setRequestSuccessMessage(null)
    setRequestedAppointment(null)

    if (!session?.accessToken) {
      navigate('/login', { state: { from: location } })
      return
    }

    if (!isPatient) {
      return
    }

    requestMutation.mutate(values)
  }

  if (!doctorProfileId) {
    return (
      <section className="bg-slate-50 py-10 sm:py-14">
        <div className="page-container">
          <ErrorState message="Doctor profile id không hợp lệ." />
        </div>
      </section>
    )
  }

  return (
    <section className="bg-slate-50 py-10 sm:py-14">
      <div className="page-container">
        <Link
          to="/doctors"
          className="inline-flex items-center gap-2 text-sm font-semibold text-care-800"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Quay lại danh sách bác sĩ
        </Link>

        {doctorQuery.isLoading ? (
          <div className="mt-8">
            <LoadingState />
          </div>
        ) : null}

        {doctorQuery.isError ? (
          <div className="mt-8">
            <ErrorState message={getApiErrorMessage(doctorQuery.error)} />
          </div>
        ) : null}

        {doctorQuery.isSuccess && doctorQuery.data ? (
          <div className="mt-8 grid gap-6 lg:grid-cols-[0.95fr_1.05fr]">
            <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex items-start gap-5">
                <div className="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-rehab-50 text-rehab-700">
                  {doctorQuery.data.avatarUrl ? (
                    <img
                      src={doctorQuery.data.avatarUrl}
                      alt={doctorQuery.data.fullName}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <Stethoscope className="h-10 w-10" aria-hidden="true" />
                  )}
                </div>
                <div>
                  <p className="text-sm font-bold text-care-800">
                    {doctorQuery.data.specialtyName}
                  </p>
                  <h1 className="mt-2 text-3xl font-bold text-slate-950">
                    {doctorQuery.data.fullName}
                  </h1>
                  <p className="mt-3 text-sm leading-6 text-slate-600">
                    {doctorQuery.data.bio ??
                      'Bác sĩ phục hồi chức năng hỗ trợ bệnh nhân sau đột quỵ xây dựng kế hoạch hồi phục an toàn.'}
                  </p>
                </div>
              </div>

              <div className="mt-6 rounded-lg border border-rehab-100 bg-rehab-50 p-5">
                <p className="text-xs font-bold uppercase text-rehab-700">
                  Lịch gần nhất
                </p>
                <p className="mt-2 text-lg font-bold text-slate-950">
                  {formatDateTime(doctorQuery.data.nextAvailableSlotStartTime)}
                </p>
                <p className="mt-1 text-sm text-slate-600">
                  đến {formatDateTime(doctorQuery.data.nextAvailableSlotEndTime)}
                </p>
              </div>

              <button type="button" className="btn-primary mt-6" onClick={openBooking}>
                <CalendarPlus className="h-4 w-4" aria-hidden="true" />
                Book appointment
              </button>
            </section>

            <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-xl font-bold text-slate-950">
                Lịch trống sắp tới
              </h2>

              {slotsQuery.isLoading ? (
                <div className="mt-5">
                  <LoadingState label="Đang tải lịch trống" />
                </div>
              ) : null}

              {slotsQuery.isError ? (
                <div className="mt-5">
                  <ErrorState message={getApiErrorMessage(slotsQuery.error)} />
                </div>
              ) : null}

              {slotsQuery.isSuccess && availableSlots.length === 0 ? (
                <div className="mt-5">
                  <EmptyState
                    icon={CalendarCheck}
                    title="Không còn lịch trống"
                    message="Bác sĩ này hiện chưa có slot Available trong tương lai."
                  />
                </div>
              ) : null}

              {slotsQuery.isSuccess && availableSlots.length > 0 ? (
                <div className="mt-5 grid gap-3 sm:grid-cols-2">
                  {availableSlots.slice(0, 6).map((slot) => (
                    <button
                      key={slot.id}
                      type="button"
                      className={[
                        'rounded-lg border p-4 text-left transition hover:border-care-300',
                        selectedSlotId === slot.id
                          ? 'border-care-400 bg-care-50'
                          : 'border-slate-200 bg-white',
                      ].join(' ')}
                      onClick={() => {
                        setValue('scheduleSlotId', slot.id)
                        openBooking()
                      }}
                    >
                      <p className="text-sm font-bold text-slate-950">
                        {formatDateTime(slot.startTime)}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        đến {formatDateTime(slot.endTime)}
                      </p>
                    </button>
                  ))}
                </div>
              ) : null}
            </section>
          </div>
        ) : null}

        {isBookingOpen ? (
          <section className="mt-6 rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
              <div>
                <h2 className="text-2xl font-bold text-slate-950">
                  Đặt lịch phục hồi sau đột quỵ
                </h2>
                <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
                  Chọn dịch vụ, slot trống và mô tả ngắn lý do để tạo lịch hẹn
                  PendingPayment trước khi xác nhận thanh toán placeholder.
                </p>
              </div>
              {createdAppointment ? (
                <StatusBadge value={createdAppointment.status} />
              ) : null}
            </div>

            {!isPatient || !patientProfileId ? (
              <div className="mt-5">
                <ErrorState
                  title="Cần tài khoản Patient"
                  message="Chỉ Patient đã đăng nhập mới có thể đặt lịch. Nếu bạn vừa đăng nhập trước khi backend cập nhật patientProfileId, hãy đăng xuất rồi đăng nhập lại."
                />
              </div>
            ) : null}

            {successMessage ? (
              <div className="mt-5 rounded-lg border border-rehab-200 bg-rehab-50 p-4 text-sm font-semibold text-rehab-800">
                {successMessage}
              </div>
            ) : null}

            {slotsQuery.isSuccess && !hasAvailableSlots ? (
              <div className="mt-5 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm font-semibold text-amber-800">
                Bác sĩ hiện chưa có lịch trống để đặt trực tiếp.
              </div>
            ) : null}

            {createdAppointment ? (
              <PaymentStep
                appointment={createdAppointment}
                canConfirmPayment={canConfirmPayment}
                isConfirming={confirmMutation.isPending}
                error={
                  confirmMutation.isError
                    ? getApiErrorMessage(confirmMutation.error)
                    : null
                }
                onConfirm={() => confirmMutation.mutate(createdAppointment.id)}
              />
            ) : (
              <form
                onSubmit={handleSubmit(onSubmit)}
                className="mt-6 grid gap-5 lg:grid-cols-[1fr_1fr]"
              >
                <label>
                  <span className="field-label">Dịch vụ</span>
                  <select className="field-input mt-2" {...register('medicalServiceId')}>
                    <option value="">Chọn dịch vụ</option>
                    {servicesQuery.data?.map((service) => (
                      <option key={service.id} value={service.id}>
                        {service.name}
                      </option>
                    ))}
                  </select>
                  {errors.medicalServiceId ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.medicalServiceId.message}
                    </span>
                  ) : null}
                </label>

                <label>
                  <span className="field-label">Lịch trống</span>
                  <select
                    className="field-input mt-2"
                    disabled={!hasAvailableSlots || slotsQuery.isLoading}
                    {...register('scheduleSlotId')}
                  >
                    <option value="">Chọn slot</option>
                    {availableSlots.map((slot) => (
                      <option key={slot.id} value={slot.id}>
                        {formatDateTime(slot.startTime)} -{' '}
                        {formatDateTime(slot.endTime)}
                      </option>
                    ))}
                  </select>
                  {errors.scheduleSlotId ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.scheduleSlotId.message}
                    </span>
                  ) : null}
                </label>

                <div className="lg:col-span-2">
                  <label>
                    <span className="field-label">Lý do đặt lịch</span>
                    <textarea
                      className="field-input mt-2 min-h-28 resize-y"
                      rows={4}
                      placeholder="Post-stroke rehabilitation consultation"
                      {...register('reason')}
                    />
                  </label>
                  <div className="mt-3 flex flex-wrap gap-2">
                    {reasonExamples.map((example) => (
                      <button
                        key={example}
                        type="button"
                        className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold text-slate-700 transition hover:border-care-300 hover:text-care-800"
                        onClick={() => setValue('reason', example)}
                      >
                        {example}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="rounded-lg border border-slate-100 bg-slate-50 p-4 lg:col-span-2">
                  <p className="text-sm font-bold text-slate-950">
                    Tóm tắt lịch hẹn
                  </p>
                  <div className="mt-3 grid gap-3 text-sm text-slate-600 sm:grid-cols-3">
                    <SummaryItem
                      label="Dịch vụ"
                      value={selectedService?.name ?? 'Chưa chọn'}
                    />
                    <SummaryItem
                      label="Chi phí"
                      value={
                        selectedService
                          ? formatCurrency(
                              selectedService.price,
                              selectedService.currency,
                            )
                          : 'N/A'
                      }
                    />
                    <SummaryItem
                      label="Thời gian"
                      value={selectedSlot ? formatDateTime(selectedSlot.startTime) : 'N/A'}
                    />
                  </div>
                </div>

                {createMutation.isError ? (
                  <div className="lg:col-span-2">
                    <ErrorState message={getApiErrorMessage(createMutation.error)} />
                  </div>
                ) : null}

                <div className="flex flex-col gap-3 sm:flex-row lg:col-span-2">
                  <button
                    type="submit"
                    className="btn-primary"
                    disabled={
                      createMutation.isPending ||
                      !isPatient ||
                      !patientProfileId ||
                      !hasAvailableSlots ||
                      servicesQuery.isLoading ||
                      slotsQuery.isLoading
                    }
                  >
                    <CalendarPlus className="h-4 w-4" aria-hidden="true" />
                    {createMutation.isPending ? 'Đang tạo lịch' : 'Tạo lịch hẹn'}
                  </button>
                  <button
                    type="button"
                    className="btn-secondary"
                    onClick={() => setIsBookingOpen(false)}
                    disabled={createMutation.isPending}
                  >
                    Hủy
                  </button>
                </div>
              </form>
            )}
          </section>
        ) : null}

        <section className="mt-6 rounded-lg border border-care-200 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
            <div>
              <p className="text-xs font-bold uppercase text-care-700">
                Flexible request
              </p>
              <h2 className="mt-2 text-2xl font-bold text-slate-950">
                Request appointment
              </h2>
              <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
                Send a preferred consultation time for Doctor review. This flow
                does not require an available schedule slot.
              </p>
            </div>
            {requestedAppointment ? (
              <StatusBadge value={requestedAppointment.status} />
            ) : null}
          </div>

          {!session?.accessToken ? (
            <div className="mt-5 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm font-semibold text-amber-800">
              Please log in as a Patient before submitting an appointment request.
            </div>
          ) : null}

          {session?.accessToken && !isPatient ? (
            <div className="mt-5">
              <ErrorState
                title="Patient account required"
                message="Only Patient users can submit appointment requests."
              />
            </div>
          ) : null}

          {requestSuccessMessage ? (
            <div className="mt-5 rounded-lg border border-rehab-200 bg-rehab-50 p-4 text-sm font-semibold text-rehab-800">
              {requestSuccessMessage}
            </div>
          ) : null}

          <form
            onSubmit={handleRequestSubmit(onRequestSubmit)}
            className="mt-6 grid gap-5 lg:grid-cols-2"
          >
            <label>
              <span className="field-label">Service</span>
              <select
                className="field-input mt-2"
                {...registerRequest('medicalServiceId')}
              >
                <option value="">Choose service</option>
                {servicesQuery.data?.map((service) => (
                  <option key={service.id} value={service.id}>
                    {service.name}
                  </option>
                ))}
              </select>
              {requestErrors.medicalServiceId ? (
                <span className="mt-2 block text-sm text-red-600">
                  {requestErrors.medicalServiceId.message}
                </span>
              ) : null}
            </label>

            <label>
              <span className="field-label">Reason</span>
              <input
                className="field-input mt-2"
                placeholder="Stroke mobility assessment"
                {...registerRequest('reason')}
              />
              {requestErrors.reason ? (
                <span className="mt-2 block text-sm text-red-600">
                  {requestErrors.reason.message}
                </span>
              ) : null}
            </label>

            <label>
              <span className="field-label">Preferred start time</span>
              <input
                className="field-input mt-2"
                type="datetime-local"
                {...registerRequest('preferredStartTime')}
              />
              {requestErrors.preferredStartTime ? (
                <span className="mt-2 block text-sm text-red-600">
                  {requestErrors.preferredStartTime.message}
                </span>
              ) : null}
            </label>

            <label>
              <span className="field-label">Preferred end time</span>
              <input
                className="field-input mt-2"
                type="datetime-local"
                {...registerRequest('preferredEndTime')}
              />
              {requestErrors.preferredEndTime ? (
                <span className="mt-2 block text-sm text-red-600">
                  {requestErrors.preferredEndTime.message}
                </span>
              ) : null}
            </label>

            {requestMutation.isError ? (
              <div className="lg:col-span-2">
                <ErrorState message={getApiErrorMessage(requestMutation.error)} />
              </div>
            ) : null}

            <div className="flex flex-col gap-3 sm:flex-row lg:col-span-2">
              <button
                type="submit"
                className="btn-primary"
                disabled={
                  requestMutation.isPending ||
                  !session?.accessToken ||
                  !isPatient ||
                  servicesQuery.isLoading
                }
              >
                <CalendarPlus className="h-4 w-4" aria-hidden="true" />
                {requestMutation.isPending
                  ? 'Submitting request'
                  : 'Request appointment'}
              </button>
              {!session?.accessToken ? (
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => navigate('/login', { state: { from: location } })}
                >
                  Log in
                </button>
              ) : null}
            </div>
          </form>
        </section>
      </div>
    </section>
  )
}

function PaymentStep({
  appointment,
  canConfirmPayment,
  isConfirming,
  error,
  onConfirm,
}: {
  appointment: Appointment
  canConfirmPayment: boolean
  isConfirming: boolean
  error: string | null
  onConfirm: () => void
}) {
  return (
    <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-5">
      <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
        <div>
          <p className="text-sm font-semibold text-amber-800">
            Appointment #{shortId(appointment.id)}
          </p>
          <h3 className="mt-2 text-xl font-bold text-slate-950">
            {appointment.status === 'Confirmed'
              ? 'Lịch hẹn đã được xác nhận'
              : 'Lịch hẹn đang chờ thanh toán'}
          </h3>
          <p className="mt-2 text-sm leading-6 text-slate-700">
            {formatDateTime(appointment.startTime)} đến{' '}
            {formatDateTime(appointment.endTime)}
          </p>
        </div>
        <StatusBadge value={appointment.status} />
      </div>

      {error ? (
        <div className="mt-4">
          <ErrorState message={error} />
        </div>
      ) : null}

      <div className="mt-5 flex flex-col gap-3 sm:flex-row">
        {canConfirmPayment ? (
          <button
            type="button"
            className="btn-primary"
            disabled={isConfirming}
            onClick={onConfirm}
          >
            <CreditCard className="h-4 w-4" aria-hidden="true" />
            {isConfirming ? 'Đang xác nhận' : 'Confirm Payment'}
          </button>
        ) : null}
        <Link to="/patient/appointments" className="btn-secondary">
          Xem lịch hẹn của tôi
        </Link>
      </div>
    </div>
  )
}

function SummaryItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
      <p className="mt-1 font-semibold text-slate-900">{value}</p>
    </div>
  )
}

function normalizeReason(value?: string | null): string | null {
  const trimmed = value?.trim() ?? ''
  return trimmed.length > 0 ? trimmed : null
}

function toIsoDateTime(value: string): string {
  return new Date(value).toISOString()
}
