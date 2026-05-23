import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertTriangle,
  Camera,
  CheckCircle2,
  Mail,
  Pencil,
  Phone,
  Save,
  Send,
  ShieldCheck,
  Stethoscope,
  X,
} from 'lucide-react'
import axios from 'axios'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import {
  getMyDoctorProfile,
  submitMyDoctorPublicProfile,
  updateMyDoctorProfile,
  uploadMyDoctorAvatar,
} from '../api/doctors'
import { API_BASE_URL } from '../api/client'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import type { DoctorSelfProfile } from '../types/doctor'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDate } from '../utils/formatters'

const MAX_IMAGE_BYTES = 2 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp']

const doctorProfileSchema = z.object({
  phoneNumber: z.string().optional(),
  bio: z.string().optional(),
})

type DoctorProfileFormValues = z.infer<typeof doctorProfileSchema>

export function DoctorProfilePage() {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarError, setAvatarError] = useState<string | null>(null)

  const profileQuery = useQuery({
    queryKey: ['doctor-profile'],
    queryFn: getMyDoctorProfile,
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DoctorProfileFormValues>({
    resolver: zodResolver(doctorProfileSchema),
    defaultValues: getProfileFormValues(profileQuery.data),
  })

  useEffect(() => {
    if (profileQuery.data) {
      reset(getProfileFormValues(profileQuery.data))
    }
  }, [profileQuery.data, reset])

  const avatarPreviewUrl = useMemo(() => {
    return avatarFile ? URL.createObjectURL(avatarFile) : null
  }, [avatarFile])

  useEffect(() => {
    return () => {
      if (avatarPreviewUrl) {
        URL.revokeObjectURL(avatarPreviewUrl)
      }
    }
  }, [avatarPreviewUrl])

  const updateMutation = useMutation({
    mutationFn: (values: DoctorProfileFormValues) =>
      updateMyDoctorProfile({
        phoneNumber: normalizeOptional(values.phoneNumber),
        bio: normalizeOptional(values.bio),
      }),
    onSuccess: async (response) => {
      queryClient.setQueryData(['doctor-profile'], response.profile)
      await queryClient.invalidateQueries({ queryKey: ['doctor-profile'] })
      reset(getProfileFormValues(response.profile))
      setIsEditing(false)
      setSuccessMessage('Doctor profile updated.')
    },
  })

  const avatarMutation = useMutation({
    mutationFn: uploadMyDoctorAvatar,
    onSuccess: async () => {
      setAvatarFile(null)
      setAvatarError(null)
      setSuccessMessage('Doctor avatar uploaded.')
      await queryClient.invalidateQueries({ queryKey: ['doctor-profile'] })
    },
  })

  const submitPublicProfileMutation = useMutation({
    mutationFn: submitMyDoctorPublicProfile,
    onSuccess: async (response) => {
      queryClient.setQueryData(['doctor-profile'], response.profile)
      await queryClient.invalidateQueries({ queryKey: ['doctor-profile'] })
      setSuccessMessage(response.message)
    },
  })

  const profile = profileQuery.data
  const displayedAvatar =
    avatarPreviewUrl ?? toPublicUrl(profile?.profileImageUrl ?? profile?.avatarUrl)
  const readinessItems = profile ? getPublicProfileReadinessItems(profile) : []
  const isPublicProfileReady = readinessItems.length === 0
  const reviewStatus = profile?.publicProfileReviewStatus ?? 'Draft'
  const submitDisabledReason = getSubmitDisabledReason(
    reviewStatus,
    isPublicProfileReady,
  )
  const submitErrorMissingItems = getSubmitErrorMissingItems(
    submitPublicProfileMutation.error,
  )

  function startEditing() {
    if (profile) {
      reset(getProfileFormValues(profile))
    }

    setSuccessMessage(null)
    setIsEditing(true)
  }

  function cancelEditing() {
    if (profile) {
      reset(getProfileFormValues(profile))
    }

    setIsEditing(false)
    setSuccessMessage(null)
  }

  function handleAvatarFileChange(file: File | null) {
    setAvatarError(null)

    if (!file) {
      setAvatarFile(null)
      return
    }

    if (!ALLOWED_IMAGE_TYPES.includes(file.type)) {
      setAvatarError('Avatar must be JPG, JPEG, PNG, or WEBP.')
      setAvatarFile(null)
      return
    }

    if (file.size > MAX_IMAGE_BYTES) {
      setAvatarError('Avatar must be 2MB or smaller.')
      setAvatarFile(null)
      return
    }

    setAvatarFile(file)
  }

  return (
    <DoctorLayout
      title="Doctor Profile"
      description="Manage safe Doctor profile fields used by the stroke rehabilitation web flow."
    >
      {profileQuery.isLoading ? <LoadingState /> : null}

      {profileQuery.isError ? (
        <ErrorState message={getApiErrorMessage(profileQuery.error)} />
      ) : null}

      {profile ? (
        <div className="space-y-5">
          {successMessage ? (
            <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-semibold text-emerald-700">
              <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
              {successMessage}
            </div>
          ) : null}

          <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
            <div className="grid gap-6 lg:grid-cols-[280px_1fr]">
              <div>
                <div className="overflow-hidden rounded-lg border border-slate-200 bg-slate-50">
                  {displayedAvatar ? (
                    <img
                      src={displayedAvatar}
                      alt={profile.fullName}
                      className="aspect-square w-full object-cover"
                    />
                  ) : (
                    <div className="flex aspect-square w-full items-center justify-center text-care-700">
                      <Stethoscope className="h-16 w-16" aria-hidden="true" />
                    </div>
                  )}
                </div>

                <label className="mt-4 block">
                  <span className="field-label">Avatar image</span>
                  <input
                    className="field-input mt-2"
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    onChange={(event) =>
                      handleAvatarFileChange(event.target.files?.[0] ?? null)
                    }
                  />
                </label>
                <p className="mt-2 text-xs text-slate-500">
                  JPG, PNG, or WEBP. Max 2MB.
                </p>

                {avatarError ? (
                  <p className="mt-3 text-sm font-semibold text-red-600">
                    {avatarError}
                  </p>
                ) : null}

                {avatarMutation.isError ? (
                  <div className="mt-3">
                    <ErrorState message={getApiErrorMessage(avatarMutation.error)} />
                  </div>
                ) : null}

                <button
                  type="button"
                  className="btn-secondary mt-4 w-full px-4 py-2"
                  disabled={!avatarFile || avatarMutation.isPending}
                  onClick={() => avatarFile && avatarMutation.mutate(avatarFile)}
                >
                  <Camera className="h-4 w-4" aria-hidden="true" />
                  {avatarMutation.isPending ? 'Uploading' : 'Upload avatar'}
                </button>
              </div>

              <div>
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <h2 className="text-2xl font-bold text-slate-950">
                      {profile.fullName}
                    </h2>
                    <p className="mt-2 text-sm font-semibold text-care-700">
                      {profile.specialtyName}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <StatusBadge value={profile.status} />
                    <StatusBadge
                      value={
                        profile.publicProfileApproved
                          ? 'ProfileApproved'
                          : 'PendingApproval'
                      }
                    />
                  </div>
                </div>

                <div className="mt-5 grid gap-4 sm:grid-cols-2">
                  <InfoItem icon={Mail} label="Email" value={profile.email} />
                  <InfoItem
                    icon={Phone}
                    label="Phone"
                    value={profile.phoneNumber ?? 'Not updated'}
                  />
                  <InfoItem
                    icon={ShieldCheck}
                    label="Email confirmed"
                    value={profile.emailConfirmed ? 'Yes' : 'No'}
                  />
                  <InfoItem
                    icon={Stethoscope}
                    label="Created"
                    value={formatDate(profile.createdAt)}
                  />
                </div>

                {isEditing ? (
                  <form
                    className="mt-6 rounded-lg border border-slate-100 bg-slate-50 p-5"
                    onSubmit={handleSubmit((values) =>
                      updateMutation.mutate(values),
                    )}
                  >
                    {updateMutation.isError ? (
                      <div className="mb-5">
                        <ErrorState
                          message={getApiErrorMessage(updateMutation.error)}
                        />
                      </div>
                    ) : null}

                    <div className="grid gap-5">
                      <label>
                        <span className="field-label">Phone number</span>
                        <input
                          className="field-input mt-2"
                          type="tel"
                          {...register('phoneNumber')}
                        />
                      </label>

                      <label>
                        <span className="field-label">Bio</span>
                        <textarea
                          className="field-input mt-2 min-h-32 resize-y"
                          rows={5}
                          {...register('bio')}
                        />
                      </label>
                      {errors.bio ? (
                        <span className="text-sm text-red-600">
                          {errors.bio.message}
                        </span>
                      ) : null}
                    </div>

                    <div className="mt-5 flex flex-wrap gap-3">
                      <button
                        type="submit"
                        className="btn-primary px-4 py-2"
                        disabled={updateMutation.isPending}
                      >
                        <Save className="h-4 w-4" aria-hidden="true" />
                        {updateMutation.isPending ? 'Saving' : 'Save'}
                      </button>
                      <button
                        type="button"
                        className="btn-secondary px-4 py-2"
                        onClick={cancelEditing}
                        disabled={updateMutation.isPending}
                      >
                        <X className="h-4 w-4" aria-hidden="true" />
                        Cancel
                      </button>
                    </div>
                  </form>
                ) : (
                  <div className="mt-6 rounded-lg border border-slate-100 bg-slate-50 p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="text-xs font-bold uppercase text-slate-500">
                          Bio
                        </p>
                        <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">
                          {profile.bio ?? 'No bio has been added yet.'}
                        </p>
                      </div>
                      <button
                        type="button"
                        className="btn-secondary shrink-0 px-4 py-2"
                        onClick={startEditing}
                      >
                        <Pencil className="h-4 w-4" aria-hidden="true" />
                        Edit
                      </button>
                    </div>
                  </div>
                )}

                {profile.yearsOfExperience != null ? (
                  <div className="mt-4 rounded-lg border border-slate-100 bg-slate-50 p-4">
                    <p className="text-xs font-bold uppercase text-slate-500">
                      Years of experience
                    </p>
                    <p className="mt-2 text-sm font-bold text-slate-950">
                      {profile.yearsOfExperience}
                    </p>
                  </div>
                ) : null}

                <section className="mt-5 rounded-lg border border-care-200 bg-care-50 p-5">
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <p className="text-xs font-bold uppercase text-care-700">
                        Public profile review
                      </p>
                      <h3 className="mt-2 text-lg font-bold text-slate-950">
                        {getReviewStatusTitle(reviewStatus)}
                      </h3>
                      <p className="mt-2 text-sm leading-6 text-slate-600">
                        Public visibility is separate from account activation.
                        Admin approval is required before this profile appears
                        on public Doctor pages.
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <StatusBadge value={reviewStatus} />
                      <StatusBadge
                        value={
                          profile.publicProfileApproved
                            ? 'ProfileApproved'
                            : 'PendingApproval'
                        }
                      />
                    </div>
                  </div>

                  {profile.publicProfileRejectionReason ? (
                    <div className="mt-4 flex gap-3 rounded-lg border border-red-200 bg-white p-4 text-sm text-red-700">
                      <AlertTriangle
                        className="mt-0.5 h-4 w-4 shrink-0"
                        aria-hidden="true"
                      />
                      <div>
                        <p className="font-bold">Rejection reason</p>
                        <p className="mt-1 leading-6">
                          {profile.publicProfileRejectionReason}
                        </p>
                      </div>
                    </div>
                  ) : null}

                  <div className="mt-4 rounded-lg border border-white bg-white p-4">
                    <p className="text-sm font-bold text-slate-950">
                      Readiness checklist
                    </p>
                    {isPublicProfileReady ? (
                      <p className="mt-2 text-sm font-semibold text-emerald-700">
                        All required public profile items are complete.
                      </p>
                    ) : (
                      <ul className="mt-3 grid gap-2 text-sm text-slate-700 sm:grid-cols-2">
                        {readinessItems.map((item) => (
                          <li key={item} className="flex items-center gap-2">
                            <span className="h-2 w-2 rounded-full bg-amber-400" />
                            {item}
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  {submitPublicProfileMutation.isError ? (
                    <div className="mt-4">
                      <ErrorState
                        message={getApiErrorMessage(
                          submitPublicProfileMutation.error,
                        )}
                      />
                      {submitErrorMissingItems.length > 0 ? (
                        <ul className="mt-3 grid gap-2 rounded-lg border border-red-100 bg-white p-4 text-sm text-red-700 sm:grid-cols-2">
                          {submitErrorMissingItems.map((item) => (
                            <li key={item}>{item}</li>
                          ))}
                        </ul>
                      ) : null}
                    </div>
                  ) : null}

                  {submitDisabledReason ? (
                    <p className="mt-4 text-sm font-semibold text-slate-600">
                      {submitDisabledReason}
                    </p>
                  ) : null}

                  <button
                    type="button"
                    className="btn-primary mt-4 px-4 py-2"
                    disabled={
                      submitPublicProfileMutation.isPending ||
                      Boolean(submitDisabledReason)
                    }
                    onClick={() => {
                      setSuccessMessage(null)
                      submitPublicProfileMutation.mutate()
                    }}
                  >
                    <Send className="h-4 w-4" aria-hidden="true" />
                    {submitPublicProfileMutation.isPending
                      ? 'Submitting'
                      : reviewStatus === 'Rejected'
                        ? 'Resubmit for review'
                        : 'Submit for review'}
                  </button>
                </section>
              </div>
            </div>
          </section>
        </div>
      ) : null}
    </DoctorLayout>
  )
}

function InfoItem({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Mail
  label: string
  value: string
}) {
  return (
    <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
      <p className="flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
        <Icon className="h-4 w-4" aria-hidden="true" />
        {label}
      </p>
      <p className="mt-2 break-words text-sm font-bold text-slate-950">
        {value}
      </p>
    </div>
  )
}

function getProfileFormValues(profile?: DoctorSelfProfile): DoctorProfileFormValues {
  return {
    phoneNumber: profile?.phoneNumber ?? '',
    bio: profile?.bio ?? '',
  }
}

function normalizeOptional(value?: string | null): string | null {
  const trimmed = value?.trim() ?? ''
  return trimmed.length > 0 ? trimmed : null
}

function getPublicProfileReadinessItems(profile: DoctorSelfProfile): string[] {
  const missingItems: string[] = []

  if (profile.status !== 'Active') {
    missingItems.push('Active account status')
  }

  if (!profile.emailConfirmed) {
    missingItems.push('Confirmed email')
  }

  if (!profile.specialtyId) {
    missingItems.push('Specialty')
  }

  if (!profile.phoneNumber?.trim()) {
    missingItems.push('Phone number')
  }

  if (!profile.bio?.trim()) {
    missingItems.push('Bio')
  }

  if (!profile.avatarUrl?.trim() && !profile.profileImageUrl?.trim()) {
    missingItems.push('Avatar/profile image')
  }

  return missingItems
}

function getSubmitDisabledReason(
  reviewStatus: string,
  isPublicProfileReady: boolean,
): string | null {
  if (reviewStatus === 'Approved') {
    return 'This public profile is already approved.'
  }

  if (reviewStatus === 'Submitted') {
    return 'This public profile is already submitted for Admin review.'
  }

  if (!isPublicProfileReady) {
    return 'Complete the readiness checklist before submitting for review.'
  }

  return null
}

function getReviewStatusTitle(reviewStatus: string): string {
  switch (reviewStatus) {
    case 'Approved':
      return 'Approved for public visibility'
    case 'Submitted':
      return 'Submitted for Admin review'
    case 'Rejected':
      return 'Rejected, updates required'
    default:
      return 'Draft profile'
  }
}

function getSubmitErrorMissingItems(error: unknown): string[] {
  if (!axios.isAxiosError(error)) {
    return []
  }

  const data = error.response?.data as { missingItems?: unknown } | undefined

  return Array.isArray(data?.missingItems)
    ? data.missingItems.filter((item): item is string => typeof item === 'string')
    : []
}

function toPublicUrl(value?: string | null): string | null {
  if (!value) {
    return null
  }

  if (value.startsWith('http://') || value.startsWith('https://')) {
    return value
  }

  return `${API_BASE_URL.replace(/\/$/, '')}${value}`
}
