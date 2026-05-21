import { zodResolver } from '@hookform/resolvers/zod'
import { LockKeyhole, LogIn, Mail } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { login } from '../api/auth'
import { getApiErrorMessage } from '../utils/apiError'
import { getDefaultRouteForRoles, storeAuth } from '../utils/authStorage'

const loginSchema = z.object({
  email: z.string().email('Email không hợp lệ.'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu.'),
})

type LoginFormValues = z.infer<typeof loginSchema>

interface LocationState {
  from?: {
    pathname?: string
  }
}

export function LoginPage() {
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const navigate = useNavigate()
  const location = useLocation()
  const locationState = location.state as LocationState | null

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  })

  async function onSubmit(values: LoginFormValues) {
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await login(values)
      const session = storeAuth(response)
      navigate(
        locationState?.from?.pathname ?? getDefaultRouteForRoles(session.roles),
        { replace: true },
      )
    } catch (requestError) {
      setError(
        getApiErrorMessage(
          requestError,
          'Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="bg-slate-50 py-12 sm:py-16">
      <div className="page-container">
        <div className="mx-auto grid max-w-5xl overflow-hidden rounded-lg border border-slate-200 bg-white shadow-soft lg:grid-cols-[0.95fr_1.05fr]">
          <div className="bg-care-800 p-8 text-white sm:p-10">
            <h1 className="text-3xl font-bold sm:text-4xl">Đăng nhập</h1>
            <p className="mt-4 text-sm leading-7 text-care-50">
              Tài khoản Active có thể truy cập các luồng dashboard, đặt lịch,
              mua sản phẩm và quản trị theo role.
            </p>
            <div className="mt-8 rounded-lg bg-white/10 p-5">
              <p className="text-sm font-semibold">RehabAI API</p>
              <p className="mt-2 text-sm leading-6 text-care-50">
                JWT access token được lưu bằng localStorage cho MVP frontend.
              </p>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="p-8 sm:p-10">
            {error ? (
              <div className="mb-5 rounded-lg border border-red-200 bg-red-50 p-4 text-sm font-medium text-red-700">
                {error}
              </div>
            ) : null}

            <div className="grid gap-5">
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
                    autoComplete="email"
                    placeholder="patient@test.com"
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
                <span className="field-label">Mật khẩu</span>
                <span className="relative mt-2 block">
                  <LockKeyhole
                    className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
                    aria-hidden="true"
                  />
                  <input
                    className="field-input pl-12"
                    type="password"
                    autoComplete="current-password"
                    placeholder="Password@123"
                    {...register('password')}
                  />
                </span>
                {errors.password ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.password.message}
                  </span>
                ) : null}
              </label>
            </div>

            <button
              type="submit"
              className="btn-primary mt-7 w-full"
              disabled={isSubmitting}
            >
              <LogIn className="h-4 w-4" aria-hidden="true" />
              {isSubmitting ? 'Đang đăng nhập' : 'Đăng nhập'}
            </button>

            <p className="mt-6 text-center text-sm text-slate-600">
              Chưa có tài khoản Patient?{' '}
              <Link className="font-semibold text-care-800" to="/register">
                Đăng ký
              </Link>
            </p>
          </form>
        </div>
      </div>
    </section>
  )
}
