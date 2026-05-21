import { apiClient } from './client'
import type {
  CreateOrderRequest,
  CustomerOrderDetail,
  CustomerOrderSummary,
  OrderActionResponse,
} from '../types/order'

export async function createOrder(
  request: CreateOrderRequest,
): Promise<OrderActionResponse> {
  const response = await apiClient.post<OrderActionResponse>('/api/orders', request)
  return response.data
}

export async function confirmOrderPayment(
  orderId: string,
): Promise<OrderActionResponse> {
  const response = await apiClient.post<OrderActionResponse>(
    `/api/orders/${orderId}/confirm-payment`,
  )
  return response.data
}

export async function getMyOrders(): Promise<CustomerOrderSummary[]> {
  const response = await apiClient.get<CustomerOrderSummary[]>(
    '/api/orders/my-orders',
  )
  return response.data
}

export async function getMyOrderById(
  orderId: string,
): Promise<CustomerOrderDetail> {
  const response = await apiClient.get<CustomerOrderDetail>(
    `/api/orders/my-orders/${orderId}`,
  )
  return response.data
}
