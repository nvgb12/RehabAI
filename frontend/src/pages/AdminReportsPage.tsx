import type { FormEvent } from 'react'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { BarChart3, CalendarDays, ClipboardList, Stethoscope } from 'lucide-react'
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

const defaultRange = getCurrentMonthRange()

export function AdminReportsPage() {
  const [rangeDraft, setRangeDraft] = useState(defaultRange)
  const [range, setRange] = useState(defaultRange)

  const reportQuery = useQuery({
    queryKey: ['admin-revenue-report', range],
    queryFn: () =>
      getRevenueReport({
        fromDate: toStartOfDayOffset(range.fromDate),
        toDate: toEndOfDayOffset(range.toDate),
      }),
  })

  const report = reportQuery.data
  const chartData = report
    ? [
        { label: 'Product revenue', value: report.productRevenue },
        { label: 'Appointment revenue', value: report.appointmentRevenue },
      ]
    : []

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setRange(rangeDraft)
  }

  return (
    <AdminLayout
      title="Revenue Reports"
      description="Báo cáo doanh thu sản phẩm và dịch vụ lịch hẹn theo khoảng ngày."
    >
      <form
        onSubmit={handleSubmit}
        className="mb-6 rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
      >
        <div className="grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
          <label className="grid gap-2">
            <span className="field-label">From date</span>
            <input
              type="date"
              className="field-input"
              value={rangeDraft.fromDate}
              onChange={(event) =>
                setRangeDraft((value) => ({
                  ...value,
                  fromDate: event.target.value,
                }))
              }
              required
            />
          </label>
          <label className="grid gap-2">
            <span className="field-label">To date</span>
            <input
              type="date"
              className="field-input"
              value={rangeDraft.toDate}
              onChange={(event) =>
                setRangeDraft((value) => ({
                  ...value,
                  toDate: event.target.value,
                }))
              }
              required
            />
          </label>
          <button type="submit" className="btn-primary">
            <CalendarDays className="h-4 w-4" aria-hidden="true" />
            Apply
          </button>
        </div>
      </form>

      {reportQuery.isLoading ? <LoadingState /> : null}

      {reportQuery.isError ? (
        <ErrorState message={getApiErrorMessage(reportQuery.error)} />
      ) : null}

      {report ? (
        <>
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
            <StatCard
              label="Product revenue"
              value={formatCurrency(report.productRevenue, report.currency)}
              description="Paid product orders, excluding pending/cancelled/deleted records."
              icon={ClipboardList}
            />
            <StatCard
              label="Appointment revenue"
              value={formatCurrency(report.appointmentRevenue, report.currency)}
              description="Confirmed/completed appointment revenue in the selected range."
              icon={Stethoscope}
            />
            <StatCard
              label="Total revenue"
              value={formatCurrency(report.totalRevenue, report.currency)}
              description="Combined revenue for the current MVP report."
              icon={BarChart3}
            />
            <StatCard
              label="Paid orders"
              value={report.paidOrderCount.toString()}
              description={`${report.confirmedAppointmentCount} confirmed appointments.`}
              icon={CalendarDays}
            />
          </div>

          <div className="mt-6 rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <h2 className="text-xl font-bold text-slate-950">
              Revenue breakdown
            </h2>
            <div className="mt-6 h-80">
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
                  <Bar dataKey="value" fill="#1559a6" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </>
      ) : null}
    </AdminLayout>
  )
}
