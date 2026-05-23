import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import {
  CheckCircle2,
  Eye,
  EyeOff,
  KeyRound,
  LockKeyhole,
  Mail,
} from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router-dom'
import { z } from 'zod'
import { resetPassword } from '../api/auth'
import { ErrorState } from '../components/ErrorState'
import { getApiErrorMessage } from '../utils/apiError'

const resetPasswordSchema = z
  .object({
    email: z.string().min(1, 'Email is required.').email('Email is invalid.'),
    token: z.string().trim().min(1, 'Password reset token is required.'),
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters.')
      .regex(/[A-Z]/, 'Password needs at least one uppercase letter.')
      .regex(/[a-z]/, 'Password needs at least one lowercase letter.')
      .regex(/[0-9]/, 'Password needs at least one number.'),
    confirmPassword: z.string().min(1, 'Please confirm the password.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const emailFromQuery = searchParams.get('email') ?? ''
  const tokenFromQuery = searchParams.get('token') ?? ''

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: emailFromQuery,
      token: tokenFromQuery,
      newPassword: '',
      confirmPassword: '',
    },
  })

  useEffect(() => {
    reset({
      email: emailFromQuery,
      token: tokenFromQuery,
      newPassword: '',
      confirmPassword: '',
    })
  }, [emailFromQuery, reset, tokenFromQuery])

  const resetMutation = useMutation({
    mutationFn: (values: ResetPasswordFormValues) =>
      resetPassword({
        email: values.email.trim(),
        token: values.token.trim(),
        newPassword: values.newPassword,
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
                Reset password
              </h1>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Use the reset token from your email to set a new password.
              </p>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Use the reset link from your email. In local development, copy the
                token from EmailLogs.MetadataJson.
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
              onSubmit={handleSubmit((values) => resetMutation.mutate(values))}
            >
              {resetMutation.isError ? (
                <ErrorState
                  message={getApiErrorMessage(
                    resetMutation.error,
                    'Reset token is invalid or expired.',
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
                <span className="field-label">Reset token</span>
                <input
                  className="field-input mt-2"
                  type="text"
                  autoComplete="one-time-code"
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
                      type={showNewPassword ? 'text' : 'password'}
                      autoComplete="new-password"
                      {...register('newPassword')}
                    />
                    <button
                      type="button"
                      className="absolute right-3 top-1/2 inline-flex h-9 w-9 -translate-y-1/2 items-center justify-center rounded-lg text-slate-500 transition hover:bg-slate-100 hover:text-care-800"
                      aria-label={
                        showNewPassword ? 'Hide password' : 'Show password'
                      }
                      onClick={() =>
                        setShowNewPassword((currentValue) => !currentValue)
                      }
                    >
                      {showNewPassword ? (
                        <EyeOff className="h-4 w-4" aria-hidden="true" />
                      ) : (
                        <Eye className="h-4 w-4" aria-hidden="true" />
                      )}
                    </button>
                  </span>
                  {errors.newPassword ? (
                    <span className="mt-2 block text-sm text-red-600">
                      {errors.newPassword.message}
                    </span>
                  ) : null}
                </label>

                <label>
                  <span className="field-label">Confirm password</span>
                  <span className="relative mt-2 block">
                    <input
                      className="field-input pr-12"
                      type={showConfirmPassword ? 'text' : 'password'}
                      autoComplete="new-password"
                      {...register('confirmPassword')}
                    />
                    <button
                      type="button"
                      className="absolute right-3 top-1/2 inline-flex h-9 w-9 -translate-y-1/2 items-center justify-center rounded-lg text-slate-500 transition hover:bg-slate-100 hover:text-care-800"
                      aria-label={
                        showConfirmPassword ? 'Hide password' : 'Show password'
                      }
                      onClick={() =>
                        setShowConfirmPassword((currentValue) => !currentValue)
                      }
                    >
                      {showConfirmPassword ? (
                        <EyeOff className="h-4 w-4" aria-hidden="true" />
                      ) : (
                        <Eye className="h-4 w-4" aria-hidden="true" />
                      )}
                    </button>
                  </span>
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
                disabled={resetMutation.isPending}
              >
                {resetMutation.isPending ? 'Resetting password' : 'Reset password'}
              </button>

              <div className="flex flex-col gap-2 text-sm sm:flex-row sm:items-center sm:justify-between">
                <Link to="/forgot-password" className="font-semibold text-care-800">
                  Request a new token
                </Link>
                <Link to="/login" className="font-semibold text-care-800">
                  Back to login
                </Link>
              </div>
            </form>
          )}
        </div>
      </div>
    </section>
  )
}
