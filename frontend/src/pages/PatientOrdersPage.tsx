import { useQuery } from '@tanstack/react-query'
import { PackageCheck, ReceiptText } from 'lucide-react'
import { useState } from 'react'
import { getMyOrderById, getMyOrders } from '../api/orderApi'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { formatCurrency, formatDateTime } from '../utils/formatters'

export function PatientOrdersPage() {
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null)
  const ordersQuery = useQuery({
    queryKey: ['my-orders'],
    queryFn: getMyOrders,
  })
  const orderDetailQuery = useQuery({
    queryKey: ['my-order-detail', selectedOrderId],
    queryFn: () => getMyOrderById(selectedOrderId!),
    enabled: Boolean(selectedOrderId),
  })

  return (
    <DashboardLayout
      title="Đơn hàng"
      description="Lịch sử mua sản phẩm phục hồi do nền tảng/bệnh viện quản lý."
    >
      {ordersQuery.isLoading ? <LoadingState /> : null}

      {ordersQuery.isError ? (
        <ErrorState message={getApiErrorMessage(ordersQuery.error)} />
      ) : null}

      {ordersQuery.isSuccess && ordersQuery.data.length === 0 ? (
        <EmptyState
          icon={PackageCheck}
          title="Chưa có đơn hàng"
          message="Các đơn mua sản phẩm hỗ trợ phục hồi sẽ xuất hiện tại đây."
        />
      ) : null}

      {ordersQuery.isSuccess && ordersQuery.data.length > 0 ? (
        <div className="grid gap-5 xl:grid-cols-[1.1fr_0.9fr]">
          <div className="space-y-4">
            {ordersQuery.data.map((order) => (
              <button
                type="button"
                key={order.orderId}
                onClick={() => setSelectedOrderId(order.orderId)}
                className="w-full rounded-lg border border-slate-200 bg-white p-5 text-left shadow-sm transition hover:border-care-300 hover:shadow-soft"
              >
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-500">
                      {order.orderNumber}
                    </p>
                    <h2 className="mt-2 text-xl font-bold text-slate-950">
                      {formatCurrency(order.totalAmount, order.currency)}
                    </h2>
                    <p className="mt-1 text-sm text-slate-500">
                      {formatDateTime(order.createdAt)}
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

          <aside className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            {!selectedOrderId ? (
              <EmptyState
                icon={ReceiptText}
                title="Chọn một đơn hàng"
                message="Bấm vào đơn hàng bên trái để xem sản phẩm, số lượng và thành tiền."
              />
            ) : null}

            {orderDetailQuery.isLoading ? (
              <LoadingState label="Đang tải chi tiết đơn hàng" />
            ) : null}

            {orderDetailQuery.isError ? (
              <ErrorState message={getApiErrorMessage(orderDetailQuery.error)} />
            ) : null}

            {orderDetailQuery.isSuccess && orderDetailQuery.data ? (
              <div>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-sm font-semibold text-slate-500">
                      Chi tiết đơn
                    </p>
                    <h2 className="mt-1 text-xl font-bold text-slate-950">
                      {orderDetailQuery.data.orderNumber}
                    </h2>
                  </div>
                  <StatusBadge value={orderDetailQuery.data.status} />
                </div>

                <p className="mt-4 text-sm leading-6 text-slate-600">
                  Địa chỉ giao: {orderDetailQuery.data.shippingAddress ?? 'N/A'}
                </p>

                <div className="mt-5 divide-y divide-slate-100">
                  {orderDetailQuery.data.items.map((item) => (
                    <div key={item.orderItemId} className="py-4">
                      <p className="font-bold text-slate-950">
                        {item.productName}
                      </p>
                      <div className="mt-2 flex items-center justify-between gap-4 text-sm text-slate-600">
                        <span>
                          {item.quantity} x{' '}
                          {formatCurrency(
                            item.unitPrice,
                            orderDetailQuery.data.currency,
                          )}
                        </span>
                        <span className="font-bold text-slate-950">
                          {formatCurrency(
                            item.subtotal,
                            orderDetailQuery.data.currency,
                          )}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="mt-5 rounded-lg bg-care-50 p-4">
                  <p className="text-sm font-semibold text-care-800">
                    Tổng thanh toán
                  </p>
                  <p className="mt-1 text-2xl font-bold text-care-900">
                    {formatCurrency(
                      orderDetailQuery.data.totalAmount,
                      orderDetailQuery.data.currency,
                    )}
                  </p>
                </div>
              </div>
            ) : null}
          </aside>
        </div>
      ) : null}
    </DashboardLayout>
  )
}
