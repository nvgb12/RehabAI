import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Camera,
  CheckCircle2,
  Mail,
  Pencil,
  Phone,
  Save,
  ShieldCheck,
  Stethoscope,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import {
  getMyDoctorProfile,
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

  const profile = profileQuery.data
  const displayedAvatar =
    avatarPreviewUrl ?? toPublicUrl(profile?.profileImageUrl ?? profile?.avatarUrl)

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

function toPublicUrl(value?: string | null): string | null {
  if (!value) {
    return null
  }

  if (value.startsWith('http://') || value.startsWith('https://')) {
    return value
  }

  return `${API_BASE_URL.replace(/\/$/, '')}${value}`
}
