import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CheckCircle2,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Save,
  UserRoundCheck,
  X,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import {
  getPatientProfile,
  updatePatientProfile,
} from '../api/patientApi'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { DashboardLayout } from '../layouts/DashboardLayout'
import type { PatientProfile } from '../types/patient'
import { getApiErrorMessage } from '../utils/apiError'
import {
  getPatientProfileId,
  updateStoredAuthProfile,
} from '../utils/authStorage'
import { formatDate } from '../utils/formatters'

const profileSchema = z.object({
  fullName: z.string().trim().min(1, 'Họ tên là bắt buộc.'),
  phoneNumber: z.string().optional(),
  dateOfBirth: z
    .string()
    .optional()
    .refine((value) => !value || isValidDateInput(value), {
      message: 'Ngày sinh không hợp lệ.',
    }),
  gender: z.string().optional(),
  address: z.string().optional(),
})

type ProfileFormValues = z.infer<typeof profileSchema>

export function PatientProfilePage() {
  const queryClient = useQueryClient()
  const patientProfileId = getPatientProfileId()
  const [isEditing, setIsEditing] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const profileQuery = useQuery({
    queryKey: ['patient-profile', patientProfileId],
    queryFn: () => getPatientProfile(patientProfileId!),
    enabled: Boolean(patientProfileId),
  })

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProfileFormValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: getProfileFormValues(profileQuery.data),
  })

  useEffect(() => {
    if (profileQuery.data) {
      reset(getProfileFormValues(profileQuery.data))
    }
  }, [profileQuery.data, reset])

  const updateMutation = useMutation({
    mutationFn: (values: ProfileFormValues) =>
      updatePatientProfile(patientProfileId!, {
        fullName: values.fullName.trim(),
        phoneNumber: normalizeOptional(values.phoneNumber),
        dateOfBirth: normalizeOptional(values.dateOfBirth),
        gender: normalizeOptional(values.gender),
        address: normalizeOptional(values.address),
      }),
    onSuccess: async (response) => {
      updateStoredAuthProfile({ fullName: response.profile.fullName })
      queryClient.setQueryData(
        ['patient-profile', patientProfileId],
        response.profile,
      )
      await queryClient.invalidateQueries({
        queryKey: ['patient-profile', patientProfileId],
      })
      reset(getProfileFormValues(response.profile))
      setIsEditing(false)
      setSuccessMessage('Hồ sơ Patient đã được cập nhật.')
    },
  })

  const profile = profileQuery.data

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

  return (
    <DashboardLayout
      title="Hồ sơ Patient"
      description="Thông tin cá nhân dùng cho luồng phục hồi sau đột quỵ và các lịch hẹn."
    >
      {!patientProfileId ? (
        <ErrorState
          title="Thiếu Patient Profile"
          message="Phiên đăng nhập hiện tại chưa có patientProfileId. Hãy đăng xuất rồi đăng nhập lại để nhận token mới."
        />
      ) : null}

      {profileQuery.isLoading ? <LoadingState /> : null}

      {profileQuery.isError ? (
        <ErrorState message={getApiErrorMessage(profileQuery.error)} />
      ) : null}

      {profileQuery.isSuccess && !profile ? (
        <EmptyState
          icon={UserRoundCheck}
          title="Chưa tìm thấy hồ sơ"
          message="Backend không trả về hồ sơ Patient cho tài khoản hiện tại."
        />
      ) : null}

      {profile ? (
        <div className="space-y-5">
          {successMessage ? (
            <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-semibold text-emerald-700">
              <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
              {successMessage}
            </div>
          ) : null}

          {isEditing ? (
            <form
              onSubmit={handleSubmit((values) => updateMutation.mutate(values))}
              className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm"
            >
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <h2 className="text-xl font-bold text-slate-950">
                    Chỉnh sửa hồ sơ
                  </h2>
                  <p className="mt-2 text-sm leading-6 text-slate-600">
                    Email dùng để đăng nhập nên chỉ hiển thị và không chỉnh sửa tại đây.
                  </p>
                </div>
                <div className="flex gap-3">
                  <button
                    type="button"
                    className="btn-secondary px-4 py-2"
                    onClick={cancelEditing}
                    disabled={updateMutation.isPending}
                  >
                    <X className="h-4 w-4" aria-hidden="true" />
                    Hủy
                  </button>
                  <button
                    type="submit"
                    className="btn-primary px-4 py-2"
                    disabled={updateMutation.isPending}
                  >
                    <Save className="h-4 w-4" aria-hidden="true" />
                    {updateMutation.isPending ? 'Đang lưu' : 'Lưu'}
                  </button>
                </div>
              </div>

              {updateMutation.isError ? (
                <div className="mt-5">
                  <ErrorState message={getApiErrorMessage(updateMutation.error)} />
                </div>
              ) : null}

              <div className="mt-6 grid gap-5 lg:grid-cols-2">
                <label>
                  <span className="field-label">Họ tên</span>
                  <input
                    className="field-input mt-2"
                    type="text"
                    autoComplete="name"
                    {...register('fullName')}
                  />
                  {errors.fullName ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.fullName.message}
                    </span>
                  ) : null}
                </label>

                <label>
                  <span className="field-label">Email</span>
                  <input
                    className="field-input mt-2 bg-slate-50 text-slate-500"
                    type="email"
                    value={profile.email}
                    disabled
                    readOnly
                  />
                </label>

                <label>
                  <span className="field-label">Số điện thoại</span>
                  <input
                    className="field-input mt-2"
                    type="tel"
                    autoComplete="tel"
                    {...register('phoneNumber')}
                  />
                </label>

                <label>
                  <span className="field-label">Ngày sinh</span>
                  <input
                    className="field-input mt-2"
                    type="date"
                    {...register('dateOfBirth')}
                  />
                  {errors.dateOfBirth ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.dateOfBirth.message}
                    </span>
                  ) : null}
                </label>

                <label>
                  <span className="field-label">Giới tính</span>
                  <select className="field-input mt-2" {...register('gender')}>
                    <option value="">Chưa cập nhật</option>
                    <option value="Female">Nữ</option>
                    <option value="Male">Nam</option>
                    <option value="Other">Khác</option>
                  </select>
                </label>

                <label className="lg:col-span-2">
                  <span className="field-label">Địa chỉ</span>
                  <textarea
                    className="field-input mt-2 min-h-28 resize-y"
                    rows={4}
                    {...register('address')}
                  />
                </label>
              </div>
            </form>
          ) : (
            <div className="grid gap-5 lg:grid-cols-[1fr_1.4fr]">
              <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
                <div className="flex items-start justify-between gap-4">
                  <span className="flex h-14 w-14 items-center justify-center rounded-lg bg-rehab-50 text-rehab-700">
                    <UserRoundCheck className="h-7 w-7" aria-hidden="true" />
                  </span>
                  <button
                    type="button"
                    className="btn-secondary px-4 py-2"
                    onClick={startEditing}
                  >
                    <Pencil className="h-4 w-4" aria-hidden="true" />
                    Edit profile
                  </button>
                </div>
                <h2 className="mt-5 text-2xl font-bold text-slate-950">
                  {profile.fullName}
                </h2>
                <div className="mt-5 space-y-3 text-sm text-slate-600">
                  <p className="flex items-center gap-2">
                    <Mail className="h-4 w-4" aria-hidden="true" />
                    {profile.email}
                  </p>
                  <p className="flex items-center gap-2">
                    <Phone className="h-4 w-4" aria-hidden="true" />
                    {profile.phoneNumber ?? 'Chưa cập nhật'}
                  </p>
                  <p className="flex items-center gap-2">
                    <MapPin className="h-4 w-4" aria-hidden="true" />
                    {profile.address ?? 'Chưa cập nhật'}
                  </p>
                </div>
              </section>

              <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
                <h2 className="text-xl font-bold text-slate-950">
                  Thông tin phục hồi
                </h2>
                <div className="mt-5 grid gap-4 sm:grid-cols-2">
                  <InfoItem label="Ngày sinh" value={formatDate(profile.dateOfBirth)} />
                  <InfoItem
                    label="Giới tính"
                    value={profile.gender ?? 'Chưa cập nhật'}
                  />
                  <InfoItem
                    label="Patient Profile ID"
                    value={profile.patientProfileId}
                  />
                  <InfoItem label="User ID" value={profile.userId} />
                </div>
              </section>
            </div>
          )}
        </div>
      ) : null}
    </DashboardLayout>
  )
}

function InfoItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
      <p className="text-xs font-semibold uppercase text-slate-500">{label}</p>
      <p className="mt-2 break-words text-sm font-bold text-slate-950">
        {value}
      </p>
    </div>
  )
}

function getProfileFormValues(profile?: PatientProfile): ProfileFormValues {
  return {
    fullName: profile?.fullName ?? '',
    phoneNumber: profile?.phoneNumber ?? '',
    dateOfBirth: toDateInputValue(profile?.dateOfBirth),
    gender: profile?.gender ?? '',
    address: profile?.address ?? '',
  }
}

function normalizeOptional(value?: string | null): string | null {
  const trimmed = value?.trim() ?? ''
  return trimmed.length > 0 ? trimmed : null
}

function toDateInputValue(value?: string | null): string {
  return value ? value.slice(0, 10) : ''
}

function isValidDateInput(value: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)

  if (!match) {
    return false
  }

  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  const date = new Date(year, month - 1, day)

  return (
    date.getFullYear() === year &&
    date.getMonth() === month - 1 &&
    date.getDate() === day
  )
}
