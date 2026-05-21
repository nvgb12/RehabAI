export interface Product {
  productId: string
  categoryId: string
  categoryName: string
  name: string
  slug: string
  description?: string | null
  price: number
  currency: string
  stockQuantity: number
  imageUrl?: string | null
}

export interface ProductFilters {
  keyword?: string
  categoryId?: string
}
