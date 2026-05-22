import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CalendarDays, Edit3, Plus, Trash2, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import {
  createDoctorScheduleSlot,
  disableDoctorScheduleSlot,
  getDoctorScheduleSlots,
  getMyDoctorProfile,
  updateDoctorScheduleSlot,
} from '../api/doctors'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import type { DoctorScheduleSlot } from '../types/doctor'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDateTime, shortId } from '../utils/formatters'

const slotBaseSchema = z.object({
  startTime: z.string().min(1, 'Start time is required.'),
  endTime: z.string().min(1, 'End time is required.'),
})

const slotSchema = slotBaseSchema
  .refine(
    (values) =>
      Boolean(values.startTime) &&
      Boolean(values.endTime) &&
      new Date(values.startTime).getTime() < new Date(values.endTime).getTime(),
    {
      message: 'Start time must be before end time.',
      path: ['endTime'],
    },
  )

const editSlotSchema = slotBaseSchema
  .extend({
    status: z.enum(['Available', 'SoftReserved', 'Booked', 'Disabled']),
  })
  .refine(
    (values) =>
      Boolean(values.startTime) &&
      Boolean(values.endTime) &&
      new Date(values.startTime).getTime() < new Date(values.endTime).getTime(),
    {
      message: 'Start time must be before end time.',
      path: ['endTime'],
    },
  )

type SlotFormValues = z.infer<typeof slotSchema>
type EditSlotFormValues = z.infer<typeof editSlotSchema>

export function DoctorSchedulePage() {
  const queryClient = useQueryClient()
  const [editingSlot, setEditingSlot] = useState<DoctorScheduleSlot | null>(null)

  const profileQuery = useQuery({
    queryKey: ['doctor-profile'],
    queryFn: getMyDoctorProfile,
  })

  const doctorProfileId = profileQuery.data?.doctorProfileId
  const slotsQuery = useQuery({
    queryKey: ['doctor-schedule-slots', doctorProfileId],
    queryFn: () => getDoctorScheduleSlots(doctorProfileId!),
    enabled: Boolean(doctorProfileId),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SlotFormValues>({
    resolver: zodResolver(slotSchema),
    defaultValues: {
      startTime: '',
      endTime: '',
    },
  })

  const editForm = useForm<EditSlotFormValues>({
    resolver: zodResolver(editSlotSchema),
    defaultValues: {
      startTime: '',
      endTime: '',
      status: 'Available',
    },
  })

  const createMutation = useMutation({
    mutationFn: (values: SlotFormValues) =>
      createDoctorScheduleSlot(doctorProfileId!, {
        startTime: toOffsetIso(values.startTime),
        endTime: toOffsetIso(values.endTime),
      }),
    onSuccess: async () => {
      reset()
      await queryClient.invalidateQueries({
        queryKey: ['doctor-schedule-slots', doctorProfileId],
      })
      await queryClient.invalidateQueries({ queryKey: ['doctor-dashboard'] })
    },
  })

  const updateMutation = useMutation({
    mutationFn: (values: EditSlotFormValues) =>
      updateDoctorScheduleSlot(doctorProfileId!, editingSlot!.id, {
        startTime: toOffsetIso(values.startTime),
        endTime: toOffsetIso(values.endTime),
        status: values.status,
      }),
    onSuccess: async () => {
      setEditingSlot(null)
      await queryClient.invalidateQueries({
        queryKey: ['doctor-schedule-slots', doctorProfileId],
      })
      await queryClient.invalidateQueries({ queryKey: ['doctor-dashboard'] })
    },
  })

  const disableMutation = useMutation({
    mutationFn: (slotId: string) =>
      disableDoctorScheduleSlot(doctorProfileId!, slotId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['doctor-schedule-slots', doctorProfileId],
      })
      await queryClient.invalidateQueries({ queryKey: ['doctor-dashboard'] })
    },
  })

  function startEditing(slot: DoctorScheduleSlot) {
    setEditingSlot(slot)
    editForm.reset({
      startTime: toDateTimeLocalValue(slot.startTime),
      endTime: toDateTimeLocalValue(slot.endTime),
      status: normalizeSlotStatus(slot.status),
    })
  }

  return (
    <DoctorLayout
      title="Doctor Schedule"
      description="Create and maintain future availability slots for stroke rehabilitation appointments."
    >
      {profileQuery.isLoading || slotsQuery.isLoading ? <LoadingState /> : null}

      {profileQuery.isError ? (
        <ErrorState message={getApiErrorMessage(profileQuery.error)} />
      ) : null}

      {slotsQuery.isError ? (
        <ErrorState message={getApiErrorMessage(slotsQuery.error)} />
      ) : null}

      {createMutation.isError ? (
        <div className="mb-5">
          <ErrorState message={getApiErrorMessage(createMutation.error)} />
        </div>
      ) : null}

      {updateMutation.isError ? (
        <div className="mb-5">
          <ErrorState message={getApiErrorMessage(updateMutation.error)} />
        </div>
      ) : null}

      {disableMutation.isError ? (
        <div className="mb-5">
          <ErrorState message={getApiErrorMessage(disableMutation.error)} />
        </div>
      ) : null}

      {doctorProfileId ? (
        <div className="space-y-6">
          <form
            className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
            onSubmit={handleSubmit((values) => createMutation.mutate(values))}
          >
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h2 className="text-xl font-bold text-slate-950">
                  Create available slot
                </h2>
                <p className="mt-1 text-sm text-slate-600">
                  New manual slots default to Available.
                </p>
              </div>
              <p className="rounded-lg bg-slate-50 px-3 py-2 text-xs font-bold text-slate-600">
                Doctor #{shortId(doctorProfileId)}
              </p>
            </div>

            <div className="mt-5 grid gap-4 md:grid-cols-[1fr_1fr_auto] md:items-start">
              <label>
                <span className="field-label">Start time</span>
                <input
                  className="field-input mt-2"
                  type="datetime-local"
                  {...register('startTime')}
                />
                {errors.startTime ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.startTime.message}
                  </span>
                ) : null}
              </label>

              <label>
                <span className="field-label">End time</span>
                <input
                  className="field-input mt-2"
                  type="datetime-local"
                  {...register('endTime')}
                />
                {errors.endTime ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.endTime.message}
                  </span>
                ) : null}
              </label>

              <button
                type="submit"
                className="btn-primary mt-7 px-4 py-3"
                disabled={createMutation.isPending}
              >
                <Plus className="h-4 w-4" aria-hidden="true" />
                {createMutation.isPending ? 'Creating' : 'Create'}
              </button>
            </div>
          </form>

          {editingSlot ? (
            <form
              className="rounded-lg border border-care-200 bg-care-50 p-5 shadow-sm"
              onSubmit={editForm.handleSubmit((values) =>
                updateMutation.mutate(values),
              )}
            >
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <h2 className="text-xl font-bold text-slate-950">
                    Edit slot #{shortId(editingSlot.id)}
                  </h2>
                  <p className="mt-1 text-sm text-slate-600">
                    Slots with active appointments may be rejected by backend rules.
                  </p>
                </div>
                <button
                  type="button"
                  className="btn-secondary px-4 py-2"
                  onClick={() => setEditingSlot(null)}
                >
                  <X className="h-4 w-4" aria-hidden="true" />
                  Cancel
                </button>
              </div>

              <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_1fr_180px_auto] lg:items-start">
                <label>
                  <span className="field-label">Start time</span>
                  <input
                    className="field-input mt-2"
                    type="datetime-local"
                    {...editForm.register('startTime')}
                  />
                </label>
                <label>
                  <span className="field-label">End time</span>
                  <input
                    className="field-input mt-2"
                    type="datetime-local"
                    {...editForm.register('endTime')}
                  />
                  {editForm.formState.errors.endTime ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {editForm.formState.errors.endTime.message}
                    </span>
                  ) : null}
                </label>
                <label>
                  <span className="field-label">Status</span>
                  <select
                    className="field-input mt-2"
                    {...editForm.register('status')}
                  >
                    <option value="Available">Available</option>
                    <option value="SoftReserved">SoftReserved</option>
                    <option value="Booked">Booked</option>
                    <option value="Disabled">Disabled</option>
                  </select>
                </label>
                <button
                  type="submit"
                  className="btn-primary mt-7 px-4 py-3"
                  disabled={updateMutation.isPending}
                >
                  Save
                </button>
              </div>
            </form>
          ) : null}

          {slotsQuery.isSuccess && slotsQuery.data.length === 0 ? (
            <EmptyState
              icon={CalendarDays}
              title="No schedule slots"
              message="Create the first future slot so patients can book stroke rehabilitation consultations."
            />
          ) : null}

          {slotsQuery.isSuccess && slotsQuery.data.length > 0 ? (
            <div className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
              <div className="overflow-x-auto">
                <table className="min-w-[900px] divide-y divide-slate-200">
                  <thead className="bg-slate-50">
                    <tr>
                      <HeaderCell>Slot</HeaderCell>
                      <HeaderCell>Start</HeaderCell>
                      <HeaderCell>End</HeaderCell>
                      <HeaderCell>Status</HeaderCell>
                      <HeaderCell>Reserved until</HeaderCell>
                      <HeaderCell>Actions</HeaderCell>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {slotsQuery.data.map((slot) => (
                      <tr key={slot.id}>
                        <BodyCell>#{shortId(slot.id)}</BodyCell>
                        <BodyCell>{formatDateTime(slot.startTime)}</BodyCell>
                        <BodyCell>{formatDateTime(slot.endTime)}</BodyCell>
                        <BodyCell>
                          <StatusBadge value={slot.status} />
                        </BodyCell>
                        <BodyCell>{formatDateTime(slot.reservedUntil)}</BodyCell>
                        <BodyCell>
                          <div className="flex flex-wrap gap-2">
                            <button
                              type="button"
                              className="rounded-lg border border-care-200 bg-care-50 px-3 py-2 text-xs font-bold text-care-800 transition hover:border-care-400"
                              onClick={() => startEditing(slot)}
                            >
                              <Edit3 className="inline h-3.5 w-3.5" aria-hidden="true" />{' '}
                              Edit
                            </button>
                            {slot.status !== 'Disabled' ? (
                              <button
                                type="button"
                                className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-bold text-red-700 transition hover:border-red-300"
                                disabled={disableMutation.isPending}
                                onClick={() => disableMutation.mutate(slot.id)}
                              >
                                <Trash2 className="inline h-3.5 w-3.5" aria-hidden="true" />{' '}
                                Disable
                              </button>
                            ) : null}
                          </div>
                        </BodyCell>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : null}
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

function toOffsetIso(value: string): string {
  return new Date(value).toISOString()
}

function toDateTimeLocalValue(value: string): string {
  const date = new Date(value)
  const offsetMs = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

function normalizeSlotStatus(status: string): EditSlotFormValues['status'] {
  if (
    status === 'Available' ||
    status === 'SoftReserved' ||
    status === 'Booked' ||
    status === 'Disabled'
  ) {
    return status
  }

  return 'Available'
}
