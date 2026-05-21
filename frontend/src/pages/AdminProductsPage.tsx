import type { FormEvent } from 'react'
import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PackagePlus, Pencil, Trash2 } from 'lucide-react'
import {
  createAdminProduct,
  deleteAdminProduct,
  getAdminProducts,
  updateAdminProduct,
} from '../api/admin'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { AdminLayout } from '../layouts/AdminLayout'
import type { AdminProduct } from '../types/admin'
import { getApiErrorMessage } from '../utils/apiError'
import { formatCurrency, shortId } from '../utils/formatters'

interface ProductFormState {
  name: string
  description: string
  categoryId: string
  price: string
  currency: string
  stockQuantity: string
  imageUrl: string
  isActive: boolean
}

const emptyProductForm: ProductFormState = {
  name: '',
  description: '',
  categoryId: '',
  price: '0',
  currency: 'VND',
  stockQuantity: '0',
  imageUrl: '',
  isActive: true,
}

export function AdminProductsPage() {
  const queryClient = useQueryClient()
  const [editingProduct, setEditingProduct] = useState<AdminProduct | null>(null)
  const [form, setForm] = useState<ProductFormState>(emptyProductForm)
  const [message, setMessage] = useState<string | null>(null)

  const productsQuery = useQuery({
    queryKey: ['admin-products'],
    queryFn: getAdminProducts,
  })

  const categories = useMemo(() => {
    const map = new Map<string, string>()
    for (const product of productsQuery.data ?? []) {
      map.set(product.categoryId, product.categoryName)
    }
    return Array.from(map, ([categoryId, categoryName]) => ({
      categoryId,
      categoryName,
    }))
  }, [productsQuery.data])

  const saveMutation = useMutation({
    mutationFn: () => {
      const request = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        categoryId: form.categoryId.trim(),
        price: Number(form.price),
        currency: form.currency.trim() || 'VND',
        stockQuantity: Number(form.stockQuantity),
        imageUrl: form.imageUrl.trim() || null,
        isActive: form.isActive,
      }

      return editingProduct
        ? updateAdminProduct(editingProduct.id, request)
        : createAdminProduct(request)
    },
    onSuccess: (response) => {
      setMessage(response.message)
      setEditingProduct(null)
      setForm(emptyProductForm)
      void queryClient.invalidateQueries({ queryKey: ['admin-products'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteAdminProduct,
    onSuccess: () => {
      setMessage('Product was soft-deleted.')
      void queryClient.invalidateQueries({ queryKey: ['admin-products'] })
    },
  })

  function startEdit(product: AdminProduct) {
    setEditingProduct(product)
    setForm({
      name: product.name,
      description: product.description ?? '',
      categoryId: product.categoryId,
      price: product.price.toString(),
      currency: product.currency || 'VND',
      stockQuantity: product.stockQuantity.toString(),
      imageUrl: product.imageUrl ?? '',
      isActive: product.isActive,
    })
    setMessage(null)
  }

  function cancelEdit() {
    setEditingProduct(null)
    setForm(emptyProductForm)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setMessage(null)
    saveMutation.mutate()
  }

  function handleDelete(product: AdminProduct) {
    const confirmed = window.confirm(`Soft delete product "${product.name}"?`)
    if (confirmed) {
      deleteMutation.mutate(product.id)
    }
  }

  return (
    <AdminLayout
      title="Admin Products"
      description="Quản lý sản phẩm phục hồi chức năng do bệnh viện/nền tảng sở hữu."
    >
      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-xl font-bold text-slate-950">
            {editingProduct ? 'Edit product' : 'Create product'}
          </h2>
          <p className="mt-2 text-sm text-slate-600">
            CategoryId hiện nhập trực tiếp vì backend chưa có endpoint quản lý
            ProductCategories.
          </p>

          <form onSubmit={handleSubmit} className="mt-5 grid gap-4">
            <label className="grid gap-2">
              <span className="field-label">Name</span>
              <input
                className="field-input"
                value={form.name}
                onChange={(event) =>
                  setForm((value) => ({ ...value, name: event.target.value }))
                }
                required
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">Description</span>
              <textarea
                className="field-input min-h-24"
                value={form.description}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    description: event.target.value,
                  }))
                }
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">CategoryId</span>
              <input
                className="field-input"
                list="admin-product-categories"
                value={form.categoryId}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    categoryId: event.target.value,
                  }))
                }
                required
              />
              <datalist id="admin-product-categories">
                {categories.map((category) => (
                  <option
                    key={category.categoryId}
                    value={category.categoryId}
                    label={category.categoryName}
                  />
                ))}
              </datalist>
            </label>

            <div className="grid gap-4 sm:grid-cols-3">
              <label className="grid gap-2">
                <span className="field-label">Price</span>
                <input
                  className="field-input"
                  type="number"
                  min="0"
                  value={form.price}
                  onChange={(event) =>
                    setForm((value) => ({ ...value, price: event.target.value }))
                  }
                  required
                />
              </label>
              <label className="grid gap-2">
                <span className="field-label">Currency</span>
                <input
                  className="field-input"
                  value={form.currency}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      currency: event.target.value,
                    }))
                  }
                />
              </label>
              <label className="grid gap-2">
                <span className="field-label">Stock</span>
                <input
                  className="field-input"
                  type="number"
                  min="0"
                  value={form.stockQuantity}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      stockQuantity: event.target.value,
                    }))
                  }
                  required
                />
              </label>
            </div>

            <label className="grid gap-2">
              <span className="field-label">ImageUrl</span>
              <input
                className="field-input"
                value={form.imageUrl}
                onChange={(event) =>
                  setForm((value) => ({ ...value, imageUrl: event.target.value }))
                }
              />
            </label>

            <label className="inline-flex items-center gap-3 text-sm font-semibold text-slate-700">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    isActive: event.target.checked,
                  }))
                }
              />
              Active
            </label>

            {message ? (
              <div className="rounded-lg border border-rehab-200 bg-rehab-50 p-3 text-sm font-semibold text-rehab-800">
                {message}
              </div>
            ) : null}

            {saveMutation.isError ? (
              <ErrorState message={getApiErrorMessage(saveMutation.error)} />
            ) : null}

            <div className="flex flex-wrap gap-3">
              <button
                type="submit"
                className="btn-primary"
                disabled={saveMutation.isPending}
              >
                <PackagePlus className="h-4 w-4" aria-hidden="true" />
                {saveMutation.isPending
                  ? 'Saving'
                  : editingProduct
                    ? 'Update product'
                    : 'Create product'}
              </button>
              {editingProduct ? (
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={cancelEdit}
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </form>
        </section>

        <section>
          {productsQuery.isLoading ? <LoadingState /> : null}

          {productsQuery.isError ? (
            <ErrorState message={getApiErrorMessage(productsQuery.error)} />
          ) : null}

          {productsQuery.isSuccess && productsQuery.data.length === 0 ? (
            <EmptyState
              icon={PackagePlus}
              title="No products"
              message="Create the first stroke rehabilitation product after choosing an existing category id."
            />
          ) : null}

          {productsQuery.isSuccess && productsQuery.data.length > 0 ? (
            <div className="grid gap-4">
              {productsQuery.data.map((product) => (
                <article
                  key={product.id}
                  className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
                >
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <p className="text-sm font-semibold text-slate-500">
                        {product.categoryName} · {shortId(product.id)}
                      </p>
                      <h3 className="mt-1 text-xl font-bold text-slate-950">
                        {product.name}
                      </h3>
                      <p className="mt-2 text-sm leading-6 text-slate-600">
                        {product.description ?? 'No description'}
                      </p>
                    </div>
                    <StatusBadge
                      value={product.isActive ? 'Active' : 'Inactive'}
                    />
                  </div>

                  <div className="mt-4 grid gap-3 text-sm text-slate-600 sm:grid-cols-3">
                    <span>{formatCurrency(product.price, product.currency)}</span>
                    <span>Stock: {product.stockQuantity}</span>
                    <span>Slug: {product.slug}</span>
                  </div>

                  <div className="mt-4 flex flex-wrap gap-3">
                    <button
                      type="button"
                      className="btn-secondary py-2"
                      onClick={() => startEdit(product)}
                    >
                      <Pencil className="h-4 w-4" aria-hidden="true" />
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn-secondary py-2 text-red-700 hover:border-red-200 hover:text-red-800"
                      onClick={() => handleDelete(product)}
                      disabled={deleteMutation.isPending}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                      Soft delete
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}
        </section>
      </div>
    </AdminLayout>
  )
}
