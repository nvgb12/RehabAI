import { PackageCheck, ShoppingCart } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { Product } from '../types/product'
import { formatCurrency } from '../utils/formatters'

interface ProductCardProps {
  product: Product
}

export function ProductCard({ product }: ProductCardProps) {
  return (
    <article className="flex h-full flex-col overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm transition hover:-translate-y-0.5 hover:shadow-soft">
      <div className="flex aspect-[4/3] items-center justify-center bg-gradient-to-br from-care-50 via-white to-rehab-50">
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <PackageCheck className="h-14 w-14 text-care-600" aria-hidden="true" />
        )}
      </div>

      <div className="flex flex-1 flex-col p-5">
        <p className="text-xs font-semibold uppercase text-rehab-700">
          {product.categoryName}
        </p>
        <h2 className="mt-2 line-clamp-2 text-lg font-bold text-slate-950">
          {product.name}
        </h2>
        <p className="mt-2 line-clamp-3 text-sm leading-6 text-slate-600">
          {product.description ?? 'Sản phẩm hỗ trợ phục hồi chức năng.'}
        </p>
        <div className="mt-5 flex items-end justify-between gap-4">
          <div>
            <p className="text-xl font-bold text-care-800">
              {formatCurrency(product.price, product.currency)}
            </p>
            <p className="text-xs font-medium text-slate-500">
              Còn {product.stockQuantity} sản phẩm
            </p>
          </div>
          <Link to={`/products/${product.productId}`} className="btn-primary px-4 py-2">
            <ShoppingCart className="h-4 w-4" aria-hidden="true" />
            Chọn
          </Link>
        </div>
      </div>
    </article>
  )
}
