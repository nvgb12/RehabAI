import { useQuery } from '@tanstack/react-query'
import { PackageSearch } from 'lucide-react'
import { useState } from 'react'
import { getProducts } from '../api/products'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { ProductCard } from '../components/ProductCard'
import { SearchBar } from '../components/SearchBar'
import { getApiErrorMessage } from '../utils/apiError'

export function ProductListPage() {
  const [keyword, setKeyword] = useState('')
  const productsQuery = useQuery({
    queryKey: ['products', keyword],
    queryFn: () => getProducts({ keyword: keyword || undefined }),
  })

  return (
    <section className="bg-slate-50 py-10 sm:py-14">
      <div className="page-container">
        <div className="grid gap-5 md:grid-cols-[1fr_420px] md:items-end">
          <div>
            <h1 className="text-3xl font-bold text-slate-950 sm:text-4xl">
              Sản phẩm phục hồi
            </h1>
            <p className="mt-3 max-w-2xl text-base leading-7 text-slate-600">
              Danh mục sản phẩm chăm sóc và hỗ trợ vận động dành cho bệnh nhân
              phục hồi sau đột quỵ.
            </p>
          </div>
          <SearchBar
            value={keyword}
            onChange={setKeyword}
            placeholder="Tìm nẹp, bóng tập tay, dụng cụ hỗ trợ..."
          />
        </div>

        <div className="mt-8">
          {productsQuery.isLoading ? <LoadingState /> : null}

          {productsQuery.isError ? (
            <ErrorState message={getApiErrorMessage(productsQuery.error)} />
          ) : null}

          {productsQuery.isSuccess && productsQuery.data.length === 0 ? (
            <EmptyState
              icon={PackageSearch}
              title="Chưa có sản phẩm phù hợp"
              message="Thử đổi từ khóa hoặc kiểm tra dữ liệu Product trong backend."
            />
          ) : null}

          {productsQuery.isSuccess && productsQuery.data.length > 0 ? (
            <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
              {productsQuery.data.map((product) => (
                <ProductCard key={product.productId} product={product} />
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </section>
  )
}
