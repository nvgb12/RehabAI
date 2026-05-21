import type { FormEvent } from 'react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Copy, Stethoscope, UserRoundPlus } from 'lucide-react'
import { createDoctorAccount, getAdminDoctors } from '../api/admin'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { AdminLayout } from '../layouts/AdminLayout'
import type { AdminDoctor, CreateDoctorResponse } from '../types/admin'
import { getApiErrorMessage } from '../utils/apiError'

interface DoctorFormState {
  fullName: string
  email: string
  phoneNumber: string
  specialtyId: string
  bio: string
  yearsOfExperience: string
}

const emptyDoctorForm: DoctorFormState = {
  fullName: '',
  email: '',
  phoneNumber: '',
  specialtyId: '',
  bio: '',
  yearsOfExperience: '0',
}

export function AdminDoctorsPage() {
  const queryClient = useQueryClient()
  const [form, setForm] = useState<DoctorFormState>(emptyDoctorForm)
  const [createdDoctor, setCreatedDoctor] =
    useState<CreateDoctorResponse | null>(null)
  const [copyMessage, setCopyMessage] = useState<string | null>(null)

  const doctorsQuery = useQuery({
    queryKey: ['admin-doctors'],
    queryFn: getAdminDoctors,
  })

  const createMutation = useMutation({
    mutationFn: () =>
      createDoctorAccount({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phoneNumber: form.phoneNumber.trim(),
        specialtyId: form.specialtyId.trim(),
        bio: form.bio.trim() || null,
        yearsOfExperience: form.yearsOfExperience
          ? Number(form.yearsOfExperience)
          : null,
      }),
    onSuccess: async (response) => {
      setCreatedDoctor(response)
      setForm(emptyDoctorForm)
      await queryClient.invalidateQueries({ queryKey: ['admin-doctors'] })
    },
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setCreatedDoctor(null)
    setCopyMessage(null)
    createMutation.mutate()
  }

  async function copyValue(value: string) {
    await navigator.clipboard.writeText(value)
    setCopyMessage('Copied to clipboard.')
  }

  return (
    <AdminLayout
      title="Admin Doctors"
      description="Create internal doctor accounts and review all doctor profiles."
    >
      <div className="grid gap-6">
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div>
              <h2 className="text-xl font-bold text-slate-950">
                Create doctor account
              </h2>
              <p className="mt-2 max-w-3xl text-sm text-slate-600">
                Enter a SpecialtyId from the seeded specialty table. A dedicated
                Specialty lookup UI can be added in a later slice.
              </p>
            </div>
          </div>

          <form
            onSubmit={handleSubmit}
            className="mt-5 grid gap-4 xl:grid-cols-[1fr_1fr_1fr_160px]"
          >
            <label className="grid gap-2">
              <span className="field-label">Full name</span>
              <input
                className="field-input"
                value={form.fullName}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    fullName: event.target.value,
                  }))
                }
                required
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">Email</span>
              <input
                className="field-input"
                type="email"
                value={form.email}
                onChange={(event) =>
                  setForm((value) => ({ ...value, email: event.target.value }))
                }
                required
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">Phone number</span>
              <input
                className="field-input"
                value={form.phoneNumber}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    phoneNumber: event.target.value,
                  }))
                }
                required
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">Experience</span>
              <input
                className="field-input"
                type="number"
                min="0"
                value={form.yearsOfExperience}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    yearsOfExperience: event.target.value,
                  }))
                }
              />
            </label>

            <label className="grid gap-2 xl:col-span-2">
              <span className="field-label">SpecialtyId</span>
              <input
                className="field-input font-mono text-sm"
                value={form.specialtyId}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    specialtyId: event.target.value,
                  }))
                }
                required
              />
            </label>

            <label className="grid gap-2 xl:col-span-2">
              <span className="field-label">Bio</span>
              <textarea
                className="field-input min-h-24"
                value={form.bio}
                onChange={(event) =>
                  setForm((value) => ({ ...value, bio: event.target.value }))
                }
                placeholder="Stroke rehabilitation doctor..."
              />
            </label>

            {createMutation.isError ? (
              <ErrorState message={getApiErrorMessage(createMutation.error)} />
            ) : null}

            <button
              type="submit"
              className="btn-primary h-fit xl:col-start-4"
              disabled={createMutation.isPending}
            >
              <UserRoundPlus className="h-4 w-4" aria-hidden="true" />
              {createMutation.isPending ? 'Creating' : 'Create doctor'}
            </button>
          </form>

          {createdDoctor ? (
            <div className="mt-5 rounded-lg border border-rehab-200 bg-rehab-50 p-5 shadow-sm">
              <h2 className="text-xl font-bold text-slate-950">
                Doctor created
              </h2>
              <p className="mt-2 text-sm font-semibold text-rehab-800">
                {createdDoctor.message}
              </p>

              <div className="mt-5 grid gap-3 text-sm text-slate-700">
                <Info label="Email" value={createdDoctor.email ?? 'N/A'} />
                <Info label="UserId" value={createdDoctor.userId ?? 'N/A'} />
                <Info
                  label="DoctorProfileId"
                  value={createdDoctor.doctorProfileId ?? 'N/A'}
                />
              </div>

              {createdDoctor.invitationToken ? (
                <CopyBlock
                  label="Invitation token"
                  value={createdDoctor.invitationToken}
                  onCopy={copyValue}
                />
              ) : null}

              {createdDoctor.passwordSetupUrl ? (
                <CopyBlock
                  label="Password setup URL"
                  value={createdDoctor.passwordSetupUrl}
                  onCopy={copyValue}
                />
              ) : null}

              {copyMessage ? (
                <p className="mt-3 text-sm font-semibold text-rehab-800">
                  {copyMessage}
                </p>
              ) : null}
            </div>
          ) : null}
        </section>

        <DoctorListPanel
          doctors={doctorsQuery.data ?? []}
          isLoading={doctorsQuery.isLoading}
          error={
            doctorsQuery.isError
              ? getApiErrorMessage(
                  doctorsQuery.error,
                  'Could not load doctors. Please try again.',
                )
              : null
          }
        />
      </div>
    </AdminLayout>
  )
}

interface DoctorListPanelProps {
  doctors: AdminDoctor[]
  isLoading: boolean
  error: string | null
}

function DoctorListPanel({ doctors, isLoading, error }: DoctorListPanelProps) {
  if (isLoading) {
    return <LoadingState label="Loading doctors" />
  }

  if (error) {
    return <ErrorState message={error} />
  }

  if (doctors.length === 0) {
    return (
      <EmptyState
        icon={Stethoscope}
        title="No doctors yet"
        message="Created doctor accounts will appear here after the admin API returns them."
      />
    )
  }

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-xl font-bold text-slate-950">Doctor accounts</h2>
          <p className="mt-1 text-sm text-slate-600">
            Includes active and pending doctor profiles. Soft-deleted records
            are excluded.
          </p>
        </div>
        <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-700">
          {doctors.length} total
        </span>
      </div>

      <div className="mt-5 overflow-x-auto rounded-lg border border-slate-200 xl:overflow-x-visible">
        <table className="min-w-[860px] divide-y divide-slate-200 text-left text-sm xl:min-w-full">
          <thead className="bg-slate-50 text-xs font-bold uppercase tracking-wide text-slate-500">
            <tr>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Doctor
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Email
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Phone
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Specialty
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Status
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Email Confirmed
              </th>
              <th scope="col" className="whitespace-nowrap px-3 py-3">
                Profile Approved
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 bg-white">
            {doctors.map((doctor) => (
              <DoctorRow key={doctor.doctorProfileId} doctor={doctor} />
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

interface DoctorRowProps {
  doctor: AdminDoctor
}

function DoctorRow({ doctor }: DoctorRowProps) {
  const shortDoctorProfileId = doctor.doctorProfileId.slice(0, 8)

  return (
    <tr className="align-middle text-slate-700">
      <td className="px-3 py-4">
        <div className="max-w-[180px]">
          <p className="truncate whitespace-nowrap font-bold text-slate-950">
            {doctor.fullName}
          </p>
          <p
            className="mt-1 w-fit rounded bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-500"
            title={doctor.doctorProfileId}
          >
            {shortDoctorProfileId}
          </p>
        </div>
      </td>
      <td className="px-3 py-4">
        <p className="max-w-[210px] truncate whitespace-nowrap" title={doctor.email}>
          {doctor.email}
        </p>
      </td>
      <td className="whitespace-nowrap px-3 py-4">
        {doctor.phoneNumber || 'N/A'}
      </td>
      <td className="px-3 py-4">
        <p
          className="max-w-[150px] truncate whitespace-nowrap"
          title={doctor.specialtyName || 'N/A'}
        >
          {doctor.specialtyName || 'N/A'}
        </p>
      </td>
      <td className="whitespace-nowrap px-3 py-4">
        <StatusBadge value={doctor.status} />
      </td>
      <td className="whitespace-nowrap px-3 py-4">
        <BooleanBadge value={doctor.emailConfirmed} />
      </td>
      <td className="whitespace-nowrap px-3 py-4">
        <BooleanBadge value={doctor.publicProfileApproved} />
      </td>
    </tr>
  )
}

interface StatusBadgeProps {
  value: string
}

function StatusBadge({ value }: StatusBadgeProps) {
  const active = value === 'Active'
  const pending = value === 'PendingPasswordSetup' || value === 'PendingEmail'

  return (
    <span
      className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${
        active
          ? 'bg-emerald-50 text-emerald-700'
          : pending
            ? 'bg-amber-50 text-amber-700'
            : 'bg-slate-100 text-slate-700'
      }`}
    >
      {value}
    </span>
  )
}

interface BooleanBadgeProps {
  value: boolean
}

function BooleanBadge({ value }: BooleanBadgeProps) {
  return (
    <span
      className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${
        value ? 'bg-rehab-50 text-rehab-800' : 'bg-slate-100 text-slate-600'
      }`}
    >
      {value ? 'Yes' : 'No'}
    </span>
  )
}

interface InfoProps {
  label: string
  value: string
}

function Info({ label, value }: InfoProps) {
  return (
    <p className="flex items-center justify-between gap-4 rounded-lg bg-white px-4 py-3">
      <span className="text-slate-500">{label}</span>
      <span className="break-all text-right font-bold text-slate-900">
        {value}
      </span>
    </p>
  )
}

interface CopyBlockProps {
  label: string
  value: string
  onCopy: (value: string) => Promise<void>
}

function CopyBlock({ label, value, onCopy }: CopyBlockProps) {
  return (
    <div className="mt-5 rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm font-bold text-slate-900">{label}</p>
        <button
          type="button"
          className="btn-secondary px-3 py-2"
          onClick={() => void onCopy(value)}
        >
          <Copy className="h-4 w-4" aria-hidden="true" />
          Copy
        </button>
      </div>
      <p className="mt-3 break-all rounded-lg bg-slate-50 p-3 text-xs text-slate-600">
        {value}
      </p>
    </div>
  )
}
