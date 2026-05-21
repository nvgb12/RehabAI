import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ClipboardList, Save } from 'lucide-react'
import type { FormEvent } from 'react'
import { useState } from 'react'
import {
  getAdminOrderById,
  getAdminOrders,
  updateAdminOrderStatus,
} from '../api/admin'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { AdminLayout } from '../layouts/AdminLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { formatCurrency, formatDateTime, shortId } from '../utils/formatters'

const allowedOrderStatuses = [
  'Paid',
  'Processing',
  'Shipped',
  'Completed',
  'Cancelled',
]

const paymentStatuses = [
  'Pending',
  'Paid',
  'Failed',
  'RefundPending',
  'Refunded',
  'RefundFailed',
]

export function AdminOrdersPage() {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState('')
  const [paymentStatus, setPaymentStatus] = useState('')
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)
  const [nextStatus, setNextStatus] = useState('Processing')
  const [message, setMessage] = useState<string | null>(null)

  const ordersQuery = useQuery({
    queryKey: ['admin-orders', status, paymentStatus],
    queryFn: () =>
      getAdminOrders({
        status: status || undefined,
        paymentStatus: paymentStatus || undefined,
      }),
  })

  const detailQuery = useQuery({
    queryKey: ['admin-order-detail', selectedOrderId],
    queryFn: () => getAdminOrderById(selectedOrderId!),
    enabled: Boolean(selectedOrderId),
  })

  const updateStatusMutation = useMutation({
    mutationFn: () =>
      updateAdminOrderStatus(selectedOrderId!, { status: nextStatus }),
    onSuccess: (response) => {
      setMessage(response.message)
      void queryClient.invalidateQueries({ queryKey: ['admin-orders'] })
      void queryClient.invalidateQueries({
        queryKey: ['admin-order-detail', selectedOrderId],
      })
    },
  })

  function handleStatusUpdate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setMessage(null)
    updateStatusMutation.mutate()
  }

  function selectOrder(orderId: string, currentStatus: string) {
    setSelectedOrderId(orderId)
    setNextStatus(
      allowedOrderStatuses.includes(currentStatus)
        ? currentStatus
        : 'Processing',
    )
    setMessage(null)
  }

  return (
    <AdminLayout
      title="Admin Orders"
      description="Theo dõi đơn hàng sản phẩm, xem chi tiết và cập nhật trạng thái xử lý."
    >
      <div className="grid gap-6 xl:grid-cols-[1fr_0.9fr]">
        <section>
          <div className="mb-4 rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
            <div className="grid gap-3 sm:grid-cols-2">
              <label className="grid gap-2">
                <span className="field-label">Order status</span>
                <select
                  className="field-input"
                  value={status}
                  onChange={(event) => setStatus(event.target.value)}
                >
                  <option value="">All statuses</option>
                  {allowedOrderStatuses.map((value) => (
                    <option key={value} value={value}>
                      {value}
                    </option>
                  ))}
                </select>
              </label>
              <label className="grid gap-2">
                <span className="field-label">Payment status</span>
                <select
                  className="field-input"
                  value={paymentStatus}
                  onChange={(event) => setPaymentStatus(event.target.value)}
                >
                  <option value="">All payments</option>
                  {paymentStatuses.map((value) => (
                    <option key={value} value={value}>
                      {value}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </div>

          {ordersQuery.isLoading ? <LoadingState /> : null}

          {ordersQuery.isError ? (
            <ErrorState message={getApiErrorMessage(ordersQuery.error)} />
          ) : null}

          {ordersQuery.isSuccess && ordersQuery.data.length === 0 ? (
            <EmptyState
              icon={ClipboardList}
              title="No orders"
              message="Paid and pending product orders will appear here."
            />
          ) : null}

          {ordersQuery.isSuccess && ordersQuery.data.length > 0 ? (
            <div className="space-y-4">
              {ordersQuery.data.map((order) => (
                <button
                  type="button"
                  key={order.orderId}
                  onClick={() => selectOrder(order.orderId, order.status)}
                  className="w-full rounded-lg border border-slate-200 bg-white p-5 text-left shadow-sm transition hover:border-care-300 hover:shadow-soft"
                >
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <p className="text-sm font-semibold text-slate-500">
                        {order.orderNumber} · {shortId(order.orderId)}
                      </p>
                      <h2 className="mt-2 text-xl font-bold text-slate-950">
                        {formatCurrency(order.totalAmount, order.currency)}
                      </h2>
                      <p className="mt-1 text-sm text-slate-500">
                        {order.patientName ??
                          order.patientEmail ??
                          'Unknown patient'}
                      </p>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <StatusBadge value={order.status} />
                      <StatusBadge value={order.paymentStatus} />
                    </div>
                  </div>
                </button>
              ))}
            </div>
          ) : null}
        </section>

        <aside className="h-fit rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          {!selectedOrderId ? (
            <EmptyState
              icon={ClipboardList}
              title="Select an order"
              message="Choose an order to view items and update processing status."
            />
          ) : null}

          {detailQuery.isLoading ? <LoadingState label="Loading order detail" /> : null}

          {detailQuery.isError ? (
            <ErrorState message={getApiErrorMessage(detailQuery.error)} />
          ) : null}

          {detailQuery.data ? (
            <div>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-sm font-semibold text-slate-500">
                    Order detail
                  </p>
                  <h2 className="mt-1 text-xl font-bold text-slate-950">
                    {detailQuery.data.orderNumber}
                  </h2>
                  <p className="mt-1 text-sm text-slate-500">
                    {formatDateTime(detailQuery.data.createdAt)}
                  </p>
                </div>
                <StatusBadge value={detailQuery.data.status} />
              </div>

              <div className="mt-4 rounded-lg bg-slate-50 p-4 text-sm leading-6 text-slate-700">
                <p>
                  Patient:{' '}
                  <strong>
                    {detailQuery.data.patientName ??
                      detailQuery.data.patientEmail ??
                      'N/A'}
                  </strong>
                </p>
                <p>Shipping: {detailQuery.data.shippingAddress ?? 'N/A'}</p>
              </div>

              <div className="mt-5 divide-y divide-slate-100">
                {detailQuery.data.items.map((item) => (
                  <div key={item.orderItemId} className="py-4">
                    <p className="font-bold text-slate-950">{item.productName}</p>
                    <div className="mt-2 flex items-center justify-between gap-4 text-sm text-slate-600">
                      <span>
                        {item.quantity} x{' '}
                        {formatCurrency(item.unitPrice, detailQuery.data.currency)}
                      </span>
                      <span className="font-bold text-slate-950">
                        {formatCurrency(item.subtotal, detailQuery.data.currency)}
                      </span>
                    </div>
                  </div>
                ))}
              </div>

              <form onSubmit={handleStatusUpdate} className="mt-5 grid gap-3">
                <label className="grid gap-2">
                  <span className="field-label">Update status</span>
                  <select
                    className="field-input"
                    value={nextStatus}
                    onChange={(event) => setNextStatus(event.target.value)}
                  >
                    {allowedOrderStatuses.map((value) => (
                      <option key={value} value={value}>
                        {value}
                      </option>
                    ))}
                  </select>
                </label>
                <button
                  type="submit"
                  className="btn-primary"
                  disabled={updateStatusMutation.isPending}
                >
                  <Save className="h-4 w-4" aria-hidden="true" />
                  {updateStatusMutation.isPending ? 'Saving' : 'Update status'}
                </button>
              </form>

              {message ? (
                <div className="mt-4 rounded-lg border border-rehab-200 bg-rehab-50 p-3 text-sm font-semibold text-rehab-800">
                  {message}
                </div>
              ) : null}

              {updateStatusMutation.isError ? (
                <div className="mt-4">
                  <ErrorState
                    message={getApiErrorMessage(updateStatusMutation.error)}
                  />
                </div>
              ) : null}
            </div>
          ) : null}
        </aside>
      </div>
    </AdminLayout>
  )
}
