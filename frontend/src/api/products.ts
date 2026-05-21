import { apiClient } from './client'
import type { Product, ProductFilters } from '../types/product'

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

export async function getProducts(filters: ProductFilters): Promise<Product[]> {
  const response = await apiClient.get<unknown>('/api/products', {
    params: filters,
  })
  return normalizeList<Product>(response.data)
}

export async function getProductById(productId: string): Promise<Product> {
  const response = await apiClient.get<Product>(`/api/products/${productId}`)
  return response.data
}
