import { apiClient } from './client'
import type {
  CurrentSubscriptionResponse,
  Subscription,
  SubscriptionMutationResponse,
  SubscriptionPlan,
} from '../types/subscription'

export async function getSubscriptionPlans(): Promise<SubscriptionPlan[]> {
  const response = await apiClient.get<SubscriptionPlan[]>(
    '/api/subscription-plans',
  )
  return response.data
}

export async function getMySubscription(): Promise<Subscription | null> {
  const response = await apiClient.get<CurrentSubscriptionResponse>(
    '/api/subscriptions/me',
  )
  return response.data.subscription
}

export async function subscribeToPlan(
  planId: string,
): Promise<SubscriptionMutationResponse> {
  const response = await apiClient.post<SubscriptionMutationResponse>(
    '/api/subscriptions/subscribe',
    { planId },
  )
  return response.data
}

export async function confirmSubscriptionPayment(
  subscriptionId: string,
): Promise<SubscriptionMutationResponse> {
  const response = await apiClient.post<SubscriptionMutationResponse>(
    `/api/subscriptions/${subscriptionId}/confirm-payment`,
  )
  return response.data
}
