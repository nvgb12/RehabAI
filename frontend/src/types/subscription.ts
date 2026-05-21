export interface SubscriptionPlan {
  planId: string
  name: string
  description: string
  price: number
  currency: string
  durationDays: number
  isActive: boolean
}

export interface Subscription {
  subscriptionId: string
  planId: string
  planName: string
  status: string
  paymentStatus: string
  startDate?: string | null
  endDate?: string | null
  price: number
  currency: string
}

export interface CurrentSubscriptionResponse {
  subscription: Subscription | null
}

export interface SubscriptionMutationResponse {
  message: string
  subscription: Subscription
}
