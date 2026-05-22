import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { CheckCircle2, KeyRound, LockKeyhole, Mail } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router-dom'
import { z } from 'zod'
import { setupDoctorPassword } from '../api/auth'
import { ErrorState } from '../components/ErrorState'
import { getApiErrorMessage } from '../utils/apiError'

const setupPasswordSchema = z
  .object({
    email: z.string().email('Email is invalid.'),
    token: z.string().trim().min(1, 'Invitation token is required.'),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters.')
      .regex(/[A-Z]/, 'Password needs at least one uppercase letter.')
      .regex(/[a-z]/, 'Password needs at least one lowercase letter.')
      .regex(/[0-9]/, 'Password needs at least one number.'),
    confirmPassword: z.string().min(1, 'Please confirm the password.'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })

type SetupPasswordFormValues = z.infer<typeof setupPasswordSchema>

export function DoctorSetupPasswordPage() {
  const [searchParams] = useSearchParams()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const emailFromQuery = searchParams.get('email') ?? ''
  const tokenFromQuery =
    searchParams.get('token') ??
    searchParams.get('invitationToken') ??
    searchParams.get('setupToken') ??
    ''

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SetupPasswordFormValues>({
    resolver: zodResolver(setupPasswordSchema),
    defaultValues: {
      email: emailFromQuery,
      token: tokenFromQuery,
      password: '',
      confirmPassword: '',
    },
  })

  useEffect(() => {
    reset({
      email: emailFromQuery,
      token: tokenFromQuery,
      password: '',
      confirmPassword: '',
    })
  }, [emailFromQuery, reset, tokenFromQuery])

  const setupMutation = useMutation({
    mutationFn: (values: SetupPasswordFormValues) =>
      setupDoctorPassword({
        email: values.email.trim(),
        token: values.token.trim(),
        password: values.password,
      }),
    onSuccess: (response) => {
      setSuccessMessage(response.message)
    },
  })

  return (
    <section className="bg-slate-50 py-12 sm:py-16">
      <div className="page-container">
        <div className="mx-auto max-w-3xl rounded-lg border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <div className="flex items-start gap-4">
            <span className="flex h-12 w-12 items-center justify-center rounded-lg bg-care-50 text-care-700">
              <KeyRound className="h-6 w-6" aria-hidden="true" />
            </span>
            <div>
              <h1 className="text-3xl font-bold text-slate-950">
                Doctor password setup
              </h1>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Complete the invitation flow before normal Doctor login.
              </p>
            </div>
          </div>

          {successMessage ? (
            <div className="mt-6 rounded-lg border border-emerald-200 bg-emerald-50 p-5 text-emerald-800">
              <div className="flex items-center gap-3 font-bold">
                <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
                {successMessage}
              </div>
              <Link to="/login" className="btn-primary mt-5 px-4 py-2">
                Go to login
              </Link>
            </div>
          ) : (
            <form
              className="mt-8 grid gap-5"
              onSubmit={handleSubmit((values) => setupMutation.mutate(values))}
            >
              {setupMutation.isError ? (
                <ErrorState
                  message={getApiErrorMessage(
                    setupMutation.error,
                    'Invitation token is invalid or expired.',
                  )}
                />
              ) : null}

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
                    readOnly={Boolean(emailFromQuery)}
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
                <span className="field-label">Invitation token</span>
                <input
                  className="field-input mt-2"
                  type="text"
                  readOnly={Boolean(tokenFromQuery)}
                  {...register('token')}
                />
                {errors.token ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.token.message}
                  </span>
                ) : null}
              </label>

              <div className="grid gap-5 md:grid-cols-2">
                <label>
                  <span className="field-label">New password</span>
                  <span className="relative mt-2 block">
                    <LockKeyhole
                      className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
                      aria-hidden="true"
                    />
                    <input
                      className="field-input pl-12"
                      type="password"
                      autoComplete="new-password"
                      {...register('password')}
                    />
                  </span>
                  {errors.password ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.password.message}
                    </span>
                  ) : null}
                </label>

                <label>
                  <span className="field-label">Confirm password</span>
                  <input
                    className="field-input mt-2"
                    type="password"
                    autoComplete="new-password"
                    {...register('confirmPassword')}
                  />
                  {errors.confirmPassword ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.confirmPassword.message}
                    </span>
                  ) : null}
                </label>
              </div>

              <button
                type="submit"
                className="btn-primary w-full"
                disabled={setupMutation.isPending}
              >
                {setupMutation.isPending ? 'Setting password' : 'Set password'}
              </button>
            </form>
          )}
        </div>
      </div>
    </section>
  )
}
