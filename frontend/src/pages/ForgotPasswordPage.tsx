import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { CheckCircle2, KeyRound, Mail } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import { forgotPassword } from '../api/auth'
import { ErrorState } from '../components/ErrorState'
import { getApiErrorMessage } from '../utils/apiError'

const forgotPasswordSchema = z.object({
  email: z.string().min(1, 'Email is required.').email('Email is invalid.'),
})

type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>

const genericSuccessMessage =
  'If the account is eligible, a password reset email has been sent.'

export function ForgotPasswordPage() {
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: '',
    },
  })

  const forgotMutation = useMutation({
    mutationFn: (values: ForgotPasswordFormValues) =>
      forgotPassword({ email: values.email.trim() }),
    onSuccess: () => {
      setSuccessMessage(genericSuccessMessage)
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
                Forgot password
              </h1>
              <p className="mt-2 text-sm leading-6 text-slate-600">
                Enter your account email to request a password reset code.
              </p>
            </div>
          </div>

          {successMessage ? (
            <div className="mt-6 rounded-lg border border-emerald-200 bg-emerald-50 p-5 text-emerald-800">
              <div className="flex items-center gap-3 font-bold">
                <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
                {successMessage}
              </div>
              <div className="mt-5 flex flex-col gap-3 sm:flex-row">
                <Link to="/reset-password" className="btn-primary px-4 py-2">
                  Enter reset token
                </Link>
                <Link to="/login" className="btn-secondary px-4 py-2">
                  Back to login
                </Link>
              </div>
            </div>
          ) : (
            <form
              className="mt-8 grid gap-5"
              onSubmit={handleSubmit((values) => forgotMutation.mutate(values))}
            >
              {forgotMutation.isError ? (
                <ErrorState
                  message={getApiErrorMessage(
                    forgotMutation.error,
                    genericSuccessMessage,
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
                    placeholder="patient@example.com"
                    {...register('email')}
                  />
                </span>
                {errors.email ? (
                  <span className="mt-2 block text-sm text-red-600">
                    {errors.email.message}
                  </span>
                ) : null}
              </label>

              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                Development note: if no email provider is configured, read the reset
                token from <span className="font-semibold">EmailLogs.MetadataJson</span>.
              </div>

              <button
                type="submit"
                className="btn-primary w-full"
                disabled={forgotMutation.isPending}
              >
                {forgotMutation.isPending ? 'Sending reset email' : 'Send reset email'}
              </button>

              <Link to="/login" className="text-sm font-semibold text-care-800">
                Back to login
              </Link>
            </form>
          )}
        </div>
      </div>
    </section>
  )
}
