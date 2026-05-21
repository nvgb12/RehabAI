import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, CreditCard, ShieldCheck, Sparkles } from 'lucide-react'
import {
  confirmSubscriptionPayment,
  getMySubscription,
  getSubscriptionPlans,
  subscribeToPlan,
} from '../api/subscriptionApi'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DashboardLayout } from '../layouts/DashboardLayout'
import type { Subscription } from '../types/subscription'
import { getApiErrorMessage } from '../utils/apiError'
import { formatCurrency, formatDate } from '../utils/formatters'

const pendingPaymentStatuses = new Set(['Pending', 'PendingPayment'])

export function PatientSubscriptionPage() {
  const queryClient = useQueryClient()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const subscriptionQuery = useQuery({
    queryKey: ['my-subscription'],
    queryFn: getMySubscription,
  })

  const plansQuery = useQuery({
    queryKey: ['subscription-plans'],
    queryFn: getSubscriptionPlans,
  })

  const subscribeMutation = useMutation({
    mutationFn: subscribeToPlan,
    onMutate: () => {
      setSuccessMessage(null)
    },
    onSuccess: (response) => {
      setSuccessMessage(
        response.message || 'Subscription đang chờ thanh toán placeholder.',
      )
      queryClient.setQueryData<Subscription | null>(
        ['my-subscription'],
        response.subscription,
      )
      void queryClient.invalidateQueries({ queryKey: ['my-subscription'] })
    },
  })

  const confirmMutation = useMutation({
    mutationFn: confirmSubscriptionPayment,
    onMutate: () => {
      setSuccessMessage(null)
    },
    onSuccess: (response) => {
      setSuccessMessage(response.message || 'Subscription đã được kích hoạt.')
      queryClient.setQueryData<Subscription | null>(
        ['my-subscription'],
        response.subscription,
      )
      void queryClient.invalidateQueries({ queryKey: ['my-subscription'] })
    },
  })

  const currentSubscription = subscriptionQuery.data
  const plans = plansQuery.data ?? []
  const hasActiveOrPendingSubscription = useMemo(
    () =>
      Boolean(
        currentSubscription &&
          (currentSubscription.status === 'Active' ||
            pendingPaymentStatuses.has(currentSubscription.status) ||
            pendingPaymentStatuses.has(currentSubscription.paymentStatus)),
      ),
    [currentSubscription],
  )
  const canConfirm =
    currentSubscription &&
    (pendingPaymentStatuses.has(currentSubscription.paymentStatus) ||
      pendingPaymentStatuses.has(currentSubscription.status))
  const subscriptionActionNotice = currentSubscription
    ? canConfirm
      ? 'Bạn đang có subscription chờ thanh toán. Hãy confirm payment placeholder trước khi đăng ký gói khác.'
      : currentSubscription.status === 'Active'
        ? 'Bạn đang có subscription Active. Đổi gói/nâng cấp có thể được xử lý trong flow sau.'
        : null
    : null

  return (
    <DashboardLayout
      title="Subscription"
      description="Quản lý gói hiện tại và thanh toán placeholder cho web flow RehabAI."
    >
      <div className="grid gap-5 lg:grid-cols-[0.9fr_1.1fr]">
        <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center gap-3">
            <div className="rounded-lg bg-rehab-50 p-2 text-rehab-700">
              <ShieldCheck className="h-5 w-5" aria-hidden="true" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-slate-950">
                Gói hiện tại
              </h2>
              <p className="text-sm text-slate-500">
                Theo dõi trạng thái subscription của tài khoản Patient.
              </p>
            </div>
          </div>

          {subscriptionQuery.isLoading ? (
            <div className="mt-5">
              <LoadingState />
            </div>
          ) : null}

          {subscriptionQuery.isError ? (
            <div className="mt-5">
              <ErrorState message={getApiErrorMessage(subscriptionQuery.error)} />
            </div>
          ) : null}

          {successMessage ? (
            <div className="mt-5 flex gap-3 rounded-lg border border-rehab-200 bg-rehab-50 p-4 text-sm font-semibold text-rehab-800">
              <CheckCircle2 className="mt-0.5 h-4 w-4" aria-hidden="true" />
              <span>{successMessage}</span>
            </div>
          ) : null}

          {subscriptionQuery.isSuccess && !currentSubscription ? (
            <div className="mt-5">
              <EmptyState
                icon={Sparkles}
                title="Chưa có subscription"
                message="Chọn một gói bên dưới để tạo subscription PendingPayment cho web flow."
              />
            </div>
          ) : null}

          {currentSubscription ? (
            <div className="mt-5 rounded-lg border border-rehab-100 bg-rehab-50 p-5">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-semibold text-rehab-700">
                    {currentSubscription.planName}
                  </p>
                  <p className="mt-2 text-2xl font-bold text-slate-950">
                    {formatCurrency(
                      currentSubscription.price,
                      currentSubscription.currency,
                    )}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <StatusBadge value={currentSubscription.status} />
                  <StatusBadge value={currentSubscription.paymentStatus} />
                </div>
              </div>

              <div className="mt-5 grid gap-3 text-sm text-slate-700">
                <InfoRow
                  label="Ngày bắt đầu"
                  value={formatDate(currentSubscription.startDate)}
                />
                <InfoRow
                  label="Ngày hết hạn"
                  value={formatDate(currentSubscription.endDate)}
                />
                <InfoRow
                  label="Trạng thái thanh toán"
                  value={currentSubscription.paymentStatus}
                />
              </div>

              {canConfirm ? (
                <button
                  type="button"
                  className="btn-primary mt-5 w-full"
                  disabled={confirmMutation.isPending}
                  onClick={() =>
                    confirmMutation.mutate(currentSubscription.subscriptionId)
                  }
                >
                  <CreditCard className="h-4 w-4" aria-hidden="true" />
                  {confirmMutation.isPending
                    ? 'Đang xác nhận'
                    : 'Confirm payment placeholder'}
                </button>
              ) : (
                <div className="mt-5 rounded-lg border border-rehab-200 bg-white p-4 text-sm font-semibold text-rehab-800">
                  Subscription đang ở trạng thái {currentSubscription.status}.
                </div>
              )}

              {confirmMutation.isError ? (
                <div className="mt-4">
                  <ErrorState message={getApiErrorMessage(confirmMutation.error)} />
                </div>
              ) : null}
            </div>
          ) : null}
        </section>

        <section>
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-bold text-slate-950">
                Gói khả dụng
              </h2>
              <p className="text-sm text-slate-500">
                Các gói đang active từ backend subscription plans.
              </p>
            </div>
          </div>

          {plansQuery.isLoading ? (
            <div className="mt-5">
              <LoadingState />
            </div>
          ) : null}

          {plansQuery.isError ? (
            <div className="mt-5">
              <ErrorState message={getApiErrorMessage(plansQuery.error)} />
            </div>
          ) : null}

          {plansQuery.isSuccess && plans.length === 0 ? (
            <div className="mt-5">
              <EmptyState
                icon={Sparkles}
                title="Chưa có plan"
                message="Backend chưa có SubscriptionPlans active."
              />
            </div>
          ) : null}

          {plans.length > 0 ? (
            <div className="mt-5 grid gap-4 md:grid-cols-2">
              {plans.map((plan) => {
                const isCurrentPlan =
                  currentSubscription?.planId.toLowerCase() ===
                  plan.planId.toLowerCase()
                const isBusy =
                  subscribeMutation.isPending || confirmMutation.isPending
                const subscribeDisabled = isBusy || hasActiveOrPendingSubscription

                return (
                  <article
                    key={plan.planId}
                    className="flex min-h-[280px] flex-col rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <h3 className="text-lg font-bold text-slate-950">
                          {plan.name}
                        </h3>
                        <p className="mt-2 text-sm leading-6 text-slate-600">
                          {plan.description}
                        </p>
                      </div>
                      <StatusBadge value={plan.isActive ? 'Active' : 'Inactive'} />
                    </div>

                    <div className="mt-auto pt-5">
                      <p className="text-2xl font-bold text-care-800">
                        {formatCurrency(plan.price, plan.currency)}
                      </p>
                      <p className="mt-1 text-sm text-slate-500">
                        Thời hạn {plan.durationDays} ngày
                      </p>

                      <button
                        type="button"
                        className="btn-primary mt-5 w-full"
                        disabled={subscribeDisabled}
                        onClick={() => subscribeMutation.mutate(plan.planId)}
                      >
                        {subscribeMutation.isPending
                          ? 'Đang đăng ký'
                          : isCurrentPlan
                            ? 'Gói hiện tại'
                            : 'Subscribe'}
                      </button>
                    </div>
                  </article>
                )
              })}
            </div>
          ) : null}

          {subscriptionActionNotice ? (
            <p className="mt-4 rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-600">
              {subscriptionActionNotice}
            </p>
          ) : null}

          {subscribeMutation.isError ? (
            <div className="mt-5">
              <ErrorState message={getApiErrorMessage(subscribeMutation.error)} />
            </div>
          ) : null}
        </section>
      </div>
    </DashboardLayout>
  )
}

interface InfoRowProps {
  label: string
  value: string
}

function InfoRow({ label, value }: InfoRowProps) {
  return (
    <p className="flex items-center justify-between gap-4 rounded-lg bg-white px-4 py-3">
      <span className="text-slate-500">{label}</span>
      <span className="text-right font-bold text-slate-900">{value}</span>
    </p>
  )
}
