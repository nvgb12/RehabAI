import type { MedicalService } from '../types/medicalService'
import type {
  AdminOrderDetail,
  AdminOrderFilters,
  AdminOrderMutationResponse,
  AdminOrderSummary,
  AdminDoctor,
  AdminProduct,
  CreateDoctorRequest,
  CreateDoctorResponse,
  MedicalServiceMutationResponse,
  ProductMutationResponse,
  RevenueReport,
  RevenueReportFilters,
  UpdateOrderStatusRequest,
  UpsertAdminProductRequest,
  UpsertMedicalServiceRequest,
} from '../types/admin'
import { apiClient } from './client'

function normalizeList<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[]
  }

  if (payload && typeof payload === 'object') {
    const shapedPayload = payload as {
      items?: T[]
      data?: T[]
      results?: T[]
    }

    return shapedPayload.items ?? shapedPayload.data ?? shapedPayload.results ?? []
  }

  return []
}

export async function getAdminProducts(): Promise<AdminProduct[]> {
  const response = await apiClient.get<unknown>('/api/admin/products')
  return normalizeList<AdminProduct>(response.data)
}

export async function getAdminProductById(
  productId: string,
): Promise<AdminProduct> {
  const response = await apiClient.get<AdminProduct>(
    `/api/admin/products/${productId}`,
  )
  return response.data
}

export async function createAdminProduct(
  request: UpsertAdminProductRequest,
): Promise<ProductMutationResponse> {
  const response = await apiClient.post<ProductMutationResponse>(
    '/api/admin/products',
    request,
  )
  return response.data
}

export async function updateAdminProduct(
  productId: string,
  request: UpsertAdminProductRequest,
): Promise<ProductMutationResponse> {
  const response = await apiClient.put<ProductMutationResponse>(
    `/api/admin/products/${productId}`,
    request,
  )
  return response.data
}

export async function deleteAdminProduct(productId: string): Promise<void> {
  await apiClient.delete(`/api/admin/products/${productId}`)
}

export async function getAdminOrders(
  filters: AdminOrderFilters,
): Promise<AdminOrderSummary[]> {
  const response = await apiClient.get<unknown>('/api/admin/orders', {
    params: filters,
  })
  return normalizeList<AdminOrderSummary>(response.data)
}

export async function getAdminOrderById(
  orderId: string,
): Promise<AdminOrderDetail> {
  const response = await apiClient.get<AdminOrderDetail>(
    `/api/admin/orders/${orderId}`,
  )
  return response.data
}

export async function updateAdminOrderStatus(
  orderId: string,
  request: UpdateOrderStatusRequest,
): Promise<AdminOrderMutationResponse> {
  const response = await apiClient.put<AdminOrderMutationResponse>(
    `/api/admin/orders/${orderId}/status`,
    request,
  )
  return response.data
}

export async function getRevenueReport(
  filters: RevenueReportFilters,
): Promise<RevenueReport> {
  const response = await apiClient.get<RevenueReport>(
    '/api/admin/reports/revenue',
    {
      params: filters,
    },
  )
  return response.data
}

export async function getAdminMedicalServices(): Promise<MedicalService[]> {
  const response = await apiClient.get<unknown>('/api/medical-services')
  return normalizeList<MedicalService>(response.data)
}

export async function createMedicalService(
  request: UpsertMedicalServiceRequest,
): Promise<MedicalServiceMutationResponse> {
  const response = await apiClient.post<MedicalServiceMutationResponse>(
    '/api/admin/medical-services',
    request,
  )
  return response.data
}

export async function updateMedicalService(
  serviceId: string,
  request: UpsertMedicalServiceRequest,
): Promise<MedicalServiceMutationResponse> {
  const response = await apiClient.put<MedicalServiceMutationResponse>(
    `/api/admin/medical-services/${serviceId}`,
    request,
  )
  return response.data
}

export async function deleteMedicalService(serviceId: string): Promise<void> {
  await apiClient.delete(`/api/admin/medical-services/${serviceId}`)
}

export async function getAdminDoctors(): Promise<AdminDoctor[]> {
  const response = await apiClient.get<unknown>('/api/admin/doctors')
  return normalizeList<AdminDoctor>(response.data)
}

export async function createDoctorAccount(
  request: CreateDoctorRequest,
): Promise<CreateDoctorResponse> {
  const response = await apiClient.post<CreateDoctorResponse>(
    '/api/admin/doctors',
    request,
  )
  return response.data
}
