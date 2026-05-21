import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  CreditCard,
  PackageCheck,
  ShieldCheck,
  ShoppingCart,
} from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import { createOrder, confirmOrderPayment } from '../api/orderApi'
import { getProductById } from '../api/products'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import type { OrderResponse } from '../types/order'
import { getApiErrorMessage } from '../utils/apiError'
import { getPatientProfileId, getStoredAuth } from '../utils/authStorage'
import { formatCurrency, shortId } from '../utils/formatters'

const orderSchema = z.object({
  quantity: z.coerce
    .number({ error: 'Quantity is required.' })
    .int('Quantity must be a whole number.')
    .min(1, 'Quantity must be greater than 0.'),
  shippingAddress: z
    .string()
    .trim()
    .min(1, 'Shipping address is required for delivery.'),
})

type OrderFormInput = z.input<typeof orderSchema>
type OrderFormValues = z.output<typeof orderSchema>

export function ProductDetailPage() {
  const { productId = '' } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const session = getStoredAuth()
  const patientProfileId = getPatientProfileId(session)
  const isPatient = Boolean(session?.roles.includes('Patient'))
  const [createdOrder, setCreatedOrder] = useState<OrderResponse | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const productQuery = useQuery({
    queryKey: ['product', productId],
    queryFn: () => getProductById(productId),
    enabled: Boolean(productId),
  })

  const {
    register,
    handleSubmit,
    control,
    setError,
    formState: { errors },
  } = useForm<OrderFormInput, unknown, OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: {
      quantity: 1,
      shippingAddress: 'Stroke rehabilitation home address',
    },
  })

  const quantityValue = useWatch({ control, name: 'quantity' })
  const quantity = Number(quantityValue) || 0
  const product = productQuery.data
  const totalAmount = product ? product.price * quantity : 0

  const createMutation = useMutation({
    mutationFn: (values: OrderFormValues) =>
      createOrder({
        patientProfileId: patientProfileId!,
        shippingAddress: values.shippingAddress.trim(),
        items: [
          {
            productId,
            quantity: values.quantity,
          },
        ],
      }),
    onSuccess: async (response) => {
      setCreatedOrder(response.order)
      setSuccessMessage(response.message)
      await queryClient.invalidateQueries({ queryKey: ['my-orders'] })
    },
  })

  const confirmMutation = useMutation({
    mutationFn: (orderId: string) => confirmOrderPayment(orderId),
    onSuccess: async (response) => {
      setCreatedOrder(response.order)
      setSuccessMessage(response.message)
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['my-orders'] }),
        queryClient.invalidateQueries({ queryKey: ['product', productId] }),
        queryClient.invalidateQueries({ queryKey: ['products'] }),
      ])
    },
  })

  function onSubmit(values: OrderFormValues) {
    setSuccessMessage(null)
    setCreatedOrder(null)

    if (!session?.accessToken) {
      navigate('/login', { state: { from: location } })
      return
    }

    if (!isPatient || !patientProfileId) {
      setError('shippingAddress', {
        message:
          'Only an authenticated Active Patient account can create an order.',
      })
      return
    }

    if (!product) {
      return
    }

    if (values.quantity > product.stockQuantity) {
      setError('quantity', {
        message: `Quantity exceeds current stock (${product.stockQuantity}).`,
      })
      return
    }

    createMutation.mutate(values)
  }

  if (!productId) {
    return (
      <section className="bg-slate-50 py-10 sm:py-14">
        <div className="page-container">
          <ErrorState message="Product id is invalid." />
        </div>
      </section>
    )
  }

  return (
    <section className="bg-slate-50 py-10 sm:py-14">
      <div className="page-container">
        <Link
          to="/products"
          className="inline-flex items-center gap-2 text-sm font-semibold text-care-800"
        >
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Back to products
        </Link>

        {productQuery.isLoading ? (
          <div className="mt-8">
            <LoadingState />
          </div>
        ) : null}

        {productQuery.isError ? (
          <div className="mt-8">
            <ErrorState message={getApiErrorMessage(productQuery.error)} />
          </div>
        ) : null}

        {product ? (
          <div className="mt-8 grid gap-6 lg:grid-cols-[0.95fr_1.05fr]">
            <section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm">
              <div className="flex aspect-[4/3] items-center justify-center bg-gradient-to-br from-care-50 via-white to-rehab-50">
                {product.imageUrl ? (
                  <img
                    src={product.imageUrl}
                    alt={product.name}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <PackageCheck
                    className="h-20 w-20 text-care-600"
                    aria-hidden="true"
                  />
                )}
              </div>

              <div className="p-6">
                <p className="text-xs font-bold uppercase text-rehab-700">
                  {product.categoryName}
                </p>
                <h1 className="mt-2 text-3xl font-bold text-slate-950">
                  {product.name}
                </h1>
                <p className="mt-4 text-sm leading-7 text-slate-600">
                  {product.description ??
                    'Hospital-owned product for stroke rehabilitation support.'}
                </p>

                <div className="mt-6 grid gap-4 sm:grid-cols-3">
                  <DetailStat
                    label="Price"
                    value={formatCurrency(product.price, product.currency)}
                  />
                  <DetailStat label="Stock" value={`${product.stockQuantity}`} />
                  <DetailStat label="Currency" value={product.currency} />
                </div>
              </div>
            </section>

            <section className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex items-start gap-3">
                <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-care-50 text-care-800">
                  <ShoppingCart className="h-5 w-5" aria-hidden="true" />
                </span>
                <div>
                  <h2 className="text-2xl font-bold text-slate-950">
                    Create product order
                  </h2>
                  <p className="mt-2 text-sm leading-6 text-slate-600">
                    Direct purchase flow for stroke rehabilitation products. Stock
                    is reduced only after payment placeholder confirmation.
                  </p>
                </div>
              </div>

              {successMessage ? (
                <div className="mt-5 rounded-lg border border-rehab-200 bg-rehab-50 p-4 text-sm font-semibold text-rehab-800">
                  {successMessage}
                </div>
              ) : null}

              {createdOrder ? (
                <OrderPaymentStep
                  order={createdOrder}
                  isConfirming={confirmMutation.isPending}
                  error={
                    confirmMutation.isError
                      ? getApiErrorMessage(confirmMutation.error)
                      : null
                  }
                  onConfirm={() => confirmMutation.mutate(createdOrder.orderId)}
                />
              ) : (
                <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-5">
                  <label>
                    <span className="field-label">Quantity</span>
                    <input
                      className="field-input mt-2"
                      type="number"
                      min={1}
                      max={product.stockQuantity}
                      {...register('quantity')}
                    />
                    {errors.quantity ? (
                      <span className="mt-2 block text-sm text-red-600">
                        {errors.quantity.message}
                      </span>
                    ) : null}
                  </label>

                  <label>
                    <span className="field-label">Shipping address</span>
                    <textarea
                      className="field-input mt-2 min-h-28 resize-y"
                      rows={4}
                      placeholder="Stroke rehabilitation home address"
                      {...register('shippingAddress')}
                    />
                    {errors.shippingAddress ? (
                      <span className="mt-2 block text-sm text-red-600">
                        {errors.shippingAddress.message}
                      </span>
                    ) : null}
                  </label>

                  <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
                    <div className="flex items-center justify-between gap-4">
                      <div>
                        <p className="text-xs font-bold uppercase text-slate-500">
                          Estimated total
                        </p>
                        <p className="mt-1 text-2xl font-bold text-care-900">
                          {formatCurrency(totalAmount, product.currency)}
                        </p>
                      </div>
                      <StatusBadge value={product.stockQuantity > 0 ? 'Active' : 'Inactive'} />
                    </div>
                  </div>

                  {createMutation.isError ? (
                    <ErrorState message={getApiErrorMessage(createMutation.error)} />
                  ) : null}

                  <button
                    type="submit"
                    className="btn-primary w-full"
                    disabled={createMutation.isPending || product.stockQuantity <= 0}
                  >
                    <ShoppingCart className="h-4 w-4" aria-hidden="true" />
                    {createMutation.isPending ? 'Creating order' : 'Create order'}
                  </button>
                </form>
              )}
            </section>
          </div>
        ) : null}
      </div>
    </section>
  )
}

function DetailStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
      <p className="text-xs font-bold uppercase text-slate-500">{label}</p>
      <p className="mt-2 text-sm font-bold text-slate-950">{value}</p>
    </div>
  )
}

function OrderPaymentStep({
  order,
  isConfirming,
  error,
  onConfirm,
}: {
  order: OrderResponse
  isConfirming: boolean
  error: string | null
  onConfirm: () => void
}) {
  const canConfirm = order.paymentStatus === 'Pending'

  return (
    <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <p className="text-sm font-semibold text-amber-800">
            Order {order.orderNumber}
          </p>
          <h3 className="mt-2 text-xl font-bold text-slate-950">
            {order.paymentStatus === 'Paid'
              ? 'Payment placeholder confirmed'
              : 'Order is waiting for payment'}
          </h3>
          <p className="mt-2 text-sm text-slate-700">
            Order ID #{shortId(order.orderId)}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <StatusBadge value={order.status} />
          <StatusBadge value={order.paymentStatus} />
        </div>
      </div>

      <div className="mt-5 divide-y divide-amber-200/70 rounded-lg bg-white/70 px-4">
        {order.items.map((item) => (
          <div key={item.orderItemId} className="py-4">
            <p className="font-bold text-slate-950">{item.productName}</p>
            <div className="mt-2 flex items-center justify-between gap-4 text-sm text-slate-600">
              <span>
                {item.quantity} x {formatCurrency(item.unitPrice, order.currency)}
              </span>
              <span className="font-bold text-slate-950">
                {formatCurrency(item.subtotal, order.currency)}
              </span>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-5 flex items-center justify-between gap-4 rounded-lg bg-white/70 p-4">
        <span className="text-sm font-bold text-slate-600">Total</span>
        <span className="text-2xl font-bold text-care-900">
          {formatCurrency(order.totalAmount, order.currency)}
        </span>
      </div>

      {error ? (
        <div className="mt-4">
          <ErrorState message={error} />
        </div>
      ) : null}

      <div className="mt-5 flex flex-col gap-3 sm:flex-row">
        {canConfirm ? (
          <button
            type="button"
            className="btn-primary"
            disabled={isConfirming}
            onClick={onConfirm}
          >
            <CreditCard className="h-4 w-4" aria-hidden="true" />
            {isConfirming ? 'Confirming payment' : 'Confirm Payment'}
          </button>
        ) : (
          <span className="inline-flex items-center gap-2 rounded-lg border border-rehab-200 bg-rehab-50 px-4 py-3 text-sm font-bold text-rehab-800">
            <ShieldCheck className="h-4 w-4" aria-hidden="true" />
            Paid / Processing
          </span>
        )}
        <Link to="/patient/orders" className="btn-secondary">
          View my orders
        </Link>
      </div>
    </div>
  )
}
