import type { MedicalService } from './medicalService'

export interface AdminProduct {
  id: string
  categoryId: string
  categoryName: string
  name: string
  slug: string
  description?: string | null
  price: number
  currency: string
  stockQuantity: number
  imageUrl?: string | null
  isActive: boolean
}

export interface UpsertAdminProductRequest {
  name: string
  description?: string | null
  categoryId: string
  price: number
  currency?: string | null
  stockQuantity: number
  imageUrl?: string | null
  isActive?: boolean
}

export interface ProductMutationResponse {
  message: string
  product: AdminProduct
}

export interface AdminOrderSummary {
  orderId: string
  orderNumber: string
  patientProfileId: string
  patientName?: string | null
  patientEmail?: string | null
  status: string
  paymentStatus: string
  totalAmount: number
  currency: string
  shippingAddress?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface AdminOrderDetail extends AdminOrderSummary {
  items: AdminOrderItem[]
}

export interface AdminOrderItem {
  orderItemId: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}

export interface AdminOrderFilters {
  status?: string
  paymentStatus?: string
  fromDate?: string
  toDate?: string
}

export interface UpdateOrderStatusRequest {
  status: string
}

export interface AdminOrderMutationResponse {
  message: string
  order: AdminOrderDetail
}

export interface RevenueReport {
  fromDate: string
  toDate: string
  productRevenue: number
  appointmentRevenue: number
  totalRevenue: number
  paidOrderCount: number
  confirmedAppointmentCount: number
  currency: string
}

export interface RevenueReportFilters {
  fromDate: string
  toDate: string
}

export interface UpsertMedicalServiceRequest {
  name: string
  description?: string | null
  durationMinutes: number
  price: number
  currency?: string | null
  isActive?: boolean
  noShowFeeEnabled: boolean
  noShowFeeAmount?: number | null
}

export interface MedicalServiceMutationResponse {
  message: string
  medicalService: MedicalService
}

export interface CreateDoctorRequest {
  fullName: string
  email: string
  phoneNumber: string
  specialtyId: string
  bio?: string | null
  yearsOfExperience?: number | null
}

export interface CreateDoctorResponse {
  message: string
  userId?: string
  doctorProfileId?: string
  email?: string
  invitationToken?: string
  passwordSetupUrl?: string
}

export interface AdminDoctor {
  doctorProfileId: string
  userId: string
  fullName: string
  email: string
  phoneNumber?: string | null
  status: string
  emailConfirmed: boolean
  specialtyId: string
  specialtyName: string
  bio?: string | null
  publicProfileApproved: boolean
  createdAt: string
  updatedAt?: string | null
  isDeleted: boolean
}
