import { zodResolver } from '@hookform/resolvers/zod'
import { Mail, Phone, UserRound, UserRoundPlus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import { registerPatient } from '../api/auth'
import { getApiErrorMessage } from '../utils/apiError'

const registerSchema = z.object({
  fullName: z.string().min(2, 'Họ tên cần ít nhất 2 ký tự.'),
  email: z.string().email('Email không hợp lệ.'),
  phoneNumber: z.string().optional(),
  password: z.string().min(8, 'Mật khẩu cần ít nhất 8 ký tự.'),
})

type RegisterFormValues = z.infer<typeof registerSchema>

export function RegisterPage() {
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
    },
  })

  async function onSubmit(values: RegisterFormValues) {
    setError(null)
    setSuccessMessage(null)
    setIsSubmitting(true)

    try {
      const response = await registerPatient(values)
      setSuccessMessage(response.message)
      reset()
    } catch (requestError) {
      setError(
        getApiErrorMessage(
          requestError,
          'Đăng ký thất bại. Vui lòng kiểm tra thông tin.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="bg-slate-50 py-12 sm:py-16">
      <div className="page-container">
        <div className="mx-auto max-w-3xl rounded-lg border border-slate-200 bg-white p-8 shadow-soft sm:p-10">
          <h1 className="text-3xl font-bold text-slate-950 sm:text-4xl">
            Đăng ký Patient
          </h1>
          <p className="mt-3 text-sm leading-7 text-slate-600">
            Tài khoản mới sẽ cần xác thực email trước khi đăng nhập và dùng các
            luồng Patient.
          </p>

          {error ? (
            <div className="mt-6 rounded-lg border border-red-200 bg-red-50 p-4 text-sm font-medium text-red-700">
              {error}
            </div>
          ) : null}

          {successMessage ? (
            <div className="mt-6 rounded-lg border border-rehab-200 bg-rehab-50 p-4 text-sm font-medium text-rehab-800">
              {successMessage}
            </div>
          ) : null}

          <form onSubmit={handleSubmit(onSubmit)} className="mt-7 grid gap-5">
            <label>
              <span className="field-label">Họ tên</span>
              <span className="relative mt-2 block">
                <UserRound
                  className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
                  aria-hidden="true"
                />
                <input
                  className="field-input pl-12"
                  placeholder="Nguyen Van A"
                  autoComplete="name"
                  {...register('fullName')}
                />
              </span>
              {errors.fullName ? (
                <span className="mt-2 block text-sm text-red-600">
                  {errors.fullName.message}
                </span>
              ) : null}
            </label>

            <div className="grid gap-5 sm:grid-cols-2">
              <label>
                <span className="field-label">Email</span>
                <span className="relative mt-2 block">
                  <Mail
                    className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
                    aria-hidden="true"
                  />
                  <input
                    className="field-input pl-12"
                    type="email"
                    placeholder="patient@test.com"
                    autoComplete="email"
                    {...register('email')}
                  />
                </span>
                {errors.email ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.email.message}
                  </span>
                ) : null}
              </label>

              <label>
                <span className="field-label">Số điện thoại</span>
                <span className="relative mt-2 block">
                  <Phone
                    className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
                    aria-hidden="true"
                  />
                  <input
                    className="field-input pl-12"
                    placeholder="0912345678"
                    autoComplete="tel"
                    {...register('phoneNumber')}
                  />
                </span>
              </label>
            </div>

            <label>
              <span className="field-label">Mật khẩu</span>
              <input
                className="field-input mt-2"
                type="password"
                placeholder="Password@123"
                autoComplete="new-password"
                {...register('password')}
              />
              {errors.password ? (
                <span className="mt-2 block text-sm text-red-600">
                  {errors.password.message}
                </span>
              ) : null}
            </label>

            <button
              type="submit"
              className="btn-primary mt-2 w-full"
              disabled={isSubmitting}
            >
              <UserRoundPlus className="h-4 w-4" aria-hidden="true" />
              {isSubmitting ? 'Đang tạo tài khoản' : 'Tạo tài khoản'}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-600">
            Đã có tài khoản?{' '}
            <Link className="font-semibold text-care-800" to="/login">
              Đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </section>
  )
}
