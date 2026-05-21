import {
  CalendarClock,
  CreditCard,
  PackageCheck,
  UserRoundCheck,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { getStoredAuth } from '../utils/authStorage'

const dashboardCards = [
  {
    title: 'Hồ sơ Patient',
    description: 'Xem thông tin cá nhân, ngày sinh, giới tính và địa chỉ.',
    to: '/patient/profile',
    icon: UserRoundCheck,
    tone: 'text-rehab-700 bg-rehab-50',
  },
  {
    title: 'Lịch hẹn',
    description: 'Theo dõi lịch phục hồi, trạng thái giữ chỗ và xác nhận.',
    to: '/patient/appointments',
    icon: CalendarClock,
    tone: 'text-care-700 bg-care-50',
  },
  {
    title: 'Đơn hàng',
    description: 'Xem lịch sử mua sản phẩm phục hồi của tài khoản hiện tại.',
    to: '/patient/orders',
    icon: PackageCheck,
    tone: 'text-amber-700 bg-amber-50',
  },
  {
    title: 'Subscription',
    description: 'Xem gói hiện tại, đăng ký plan và confirm payment placeholder.',
    to: '/patient/subscription',
    icon: CreditCard,
    tone: 'text-slate-700 bg-slate-100',
  },
]

export function PatientDashboardPage() {
  const session = getStoredAuth()

  return (
    <DashboardLayout
      title="Patient Dashboard"
      description={`Xin chào ${session?.fullName ?? 'Patient'}, chọn một khu vực để quản lý hành trình phục hồi sau đột quỵ.`}
    >
      <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
        {dashboardCards.map((card) => (
          <Link
            key={card.to}
            to={card.to}
            className="group rounded-lg border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-care-300 hover:shadow-soft"
          >
            <span
              className={`flex h-11 w-11 items-center justify-center rounded-lg ${card.tone}`}
            >
              <card.icon className="h-6 w-6" aria-hidden="true" />
            </span>
            <h2 className="mt-4 text-lg font-bold text-slate-950">
              {card.title}
            </h2>
            <p className="mt-2 text-sm leading-6 text-slate-600">
              {card.description}
            </p>
            <p className="mt-4 text-sm font-bold text-care-800">
              Mở trang
              <span className="ml-1 transition group-hover:ml-2">→</span>
            </p>
          </Link>
        ))}
      </div>
    </DashboardLayout>
  )
}
