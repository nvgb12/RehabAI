import { useQuery } from '@tanstack/react-query'
import { BarChart3, ClipboardList, Stethoscope, WalletCards } from 'lucide-react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getRevenueReport } from '../api/admin'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatCard } from '../components/StatCard'
import { AdminLayout } from '../layouts/AdminLayout'
import { getApiErrorMessage } from '../utils/apiError'
import {
  getCurrentMonthRange,
  toEndOfDayOffset,
  toStartOfDayOffset,
} from '../utils/dateRange'
import { formatCurrency } from '../utils/formatters'

const currentMonth = getCurrentMonthRange()

export function AdminDashboardPage() {
  const revenueQuery = useQuery({
    queryKey: ['admin-revenue-summary', currentMonth],
    queryFn: () =>
      getRevenueReport({
        fromDate: toStartOfDayOffset(currentMonth.fromDate),
        toDate: toEndOfDayOffset(currentMonth.toDate),
      }),
  })

  const report = revenueQuery.data
  const chartData = report
    ? [
        { label: 'Products', revenue: report.productRevenue },
        { label: 'Appointments', revenue: report.appointmentRevenue },
      ]
    : []

  return (
    <AdminLayout
      title="Admin Dashboard"
      description="Tổng quan doanh thu và các module quản trị chính của RehabAI."
    >
      {revenueQuery.isLoading ? <LoadingState /> : null}

      {revenueQuery.isError ? (
        <ErrorState message={getApiErrorMessage(revenueQuery.error)} />
      ) : null}

      {report ? (
        <>
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
            <StatCard
              label="Product revenue"
              value={formatCurrency(report.productRevenue, report.currency)}
              description="Doanh thu từ đơn hàng sản phẩm đã thanh toán trong tháng hiện tại."
              icon={WalletCards}
            />
            <StatCard
              label="Appointment revenue"
              value={formatCurrency(report.appointmentRevenue, report.currency)}
              description="Doanh thu dịch vụ lịch hẹn đã xác nhận/hoàn thành."
              icon={Stethoscope}
            />
            <StatCard
              label="Total revenue"
              value={formatCurrency(report.totalRevenue, report.currency)}
              description="Tổng doanh thu MVP trong khoảng báo cáo hiện tại."
              icon={BarChart3}
            />
            <StatCard
              label="Paid orders"
              value={report.paidOrderCount.toString()}
              description="Số đơn hàng sản phẩm đã thanh toán."
              icon={ClipboardList}
            />
          </div>

          <div className="mt-6 rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <h2 className="text-xl font-bold text-slate-950">
                  Current month revenue
                </h2>
                <p className="mt-1 text-sm text-slate-600">
                  {currentMonth.fromDate} đến {currentMonth.toDate}
                </p>
              </div>
              <p className="text-sm font-semibold text-care-700">
                {report.confirmedAppointmentCount} confirmed appointments
              </p>
            </div>

            <div className="mt-6 h-72">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="label" stroke="#64748b" />
                  <YAxis stroke="#64748b" />
                  <Tooltip
                    formatter={(value) =>
                      formatCurrency(Number(value), report.currency)
                    }
                  />
                  <Bar dataKey="revenue" fill="#259890" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </>
      ) : null}
    </AdminLayout>
  )
}
