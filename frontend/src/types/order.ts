export interface CustomerOrderSummary {
  orderId: string
  orderNumber: string
  createdAt: string
  totalAmount: number
  currency: string
  status: string
  paymentStatus: string
}

export interface CustomerOrderDetail extends CustomerOrderSummary {
  updatedAt?: string | null
  shippingAddress?: string | null
  items: CustomerOrderItem[]
}

export interface CustomerOrderItem {
  orderItemId: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface CreateOrderRequest {
  patientProfileId: string
  items: CreateOrderItemRequest[]
  shippingAddress?: string | null
}

export interface CreateOrderItemRequest {
  productId: string
  quantity: number
}

export interface OrderResponse {
  orderId: string
  patientProfileId: string
  patientUserId: string
  orderNumber: string
  status: string
  paymentStatus: string
  totalAmount: number
  currency: string
  shippingAddress?: string | null
  items: OrderItemResponse[]
}

export interface OrderItemResponse {
  orderItemId: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface OrderActionResponse {
  message: string
  order: OrderResponse
}
