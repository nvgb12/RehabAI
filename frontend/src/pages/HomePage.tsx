import {
  ArrowRight,
  CalendarDays,
  HeartPulse,
  PackageCheck,
  ShieldCheck,
  Stethoscope,
} from 'lucide-react'
import { Link } from 'react-router-dom'

const careSteps = [
  {
    title: 'Tư vấn phục hồi',
    description: 'Kết nối với bác sĩ phục hồi chức năng có lịch trống.',
    icon: Stethoscope,
  },
  {
    title: 'Sản phẩm hỗ trợ',
    description: 'Dụng cụ và vật tư chăm sóc do bệnh viện quản lý.',
    icon: PackageCheck,
  },
  {
    title: 'Theo dõi lịch hẹn',
    description: 'Luồng đặt lịch, giữ chỗ và xác nhận thanh toán MVP.',
    icon: CalendarDays,
  },
]

export function HomePage() {
  return (
    <>
      <section className="bg-white py-12 sm:py-16 lg:py-20">
        <div className="page-container grid items-center gap-10 lg:grid-cols-[1.05fr_0.95fr]">
          <div>
            <h1 className="max-w-3xl text-4xl font-bold leading-tight text-slate-950 sm:text-5xl lg:text-6xl">
              RehabAI
            </h1>
            <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-600">
              Đặt lịch phục hồi sau đột quỵ, tìm bác sĩ phù hợp và mua sản
              phẩm hỗ trợ chăm sóc trong một trải nghiệm web thống nhất.
            </p>
            <div className="mt-8 flex flex-col gap-3 sm:flex-row">
              <Link to="/doctors" className="btn-primary">
                Tìm bác sĩ
                <ArrowRight className="h-4 w-4" aria-hidden="true" />
              </Link>
              <Link to="/products" className="btn-secondary">
                Xem sản phẩm
              </Link>
            </div>
          </div>

          <div className="rounded-lg border border-care-100 bg-care-50 p-5 shadow-soft">
            <div className="rounded-lg bg-white p-5">
              <div className="flex items-center gap-4">
                <span className="flex h-14 w-14 items-center justify-center rounded-lg bg-rehab-100 text-rehab-700">
                  <HeartPulse className="h-7 w-7" aria-hidden="true" />
                </span>
                <div>
                  <p className="text-sm font-semibold text-slate-500">
                    Stroke recovery
                  </p>
                  <h2 className="text-xl font-bold text-slate-950">
                    Hành trình phục hồi có lịch rõ ràng
                  </h2>
                </div>
              </div>

              <div className="mt-6 grid gap-3">
                {careSteps.map((step) => (
                  <div
                    key={step.title}
                    className="flex items-start gap-3 rounded-lg border border-slate-100 bg-slate-50 p-4"
                  >
                    <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-white text-care-700">
                      <step.icon className="h-5 w-5" aria-hidden="true" />
                    </span>
                    <div>
                      <h3 className="text-sm font-bold text-slate-950">
                        {step.title}
                      </h3>
                      <p className="mt-1 text-sm leading-6 text-slate-600">
                        {step.description}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="border-y border-slate-100 bg-slate-50 py-10">
        <div className="page-container grid gap-4 sm:grid-cols-3">
          <div className="rounded-lg bg-white p-5 shadow-sm">
            <ShieldCheck className="h-6 w-6 text-rehab-700" aria-hidden="true" />
            <p className="mt-3 text-sm font-semibold text-slate-950">
              JWT protected flows
            </p>
            <p className="mt-2 text-sm leading-6 text-slate-600">
              Patient, Admin và Doctor được tách role từ backend.
            </p>
          </div>
          <div className="rounded-lg bg-white p-5 shadow-sm">
            <CalendarDays className="h-6 w-6 text-care-700" aria-hidden="true" />
            <p className="mt-3 text-sm font-semibold text-slate-950">
              Appointment MVP
            </p>
            <p className="mt-2 text-sm leading-6 text-slate-600">
              Slot, đặt lịch, giữ chỗ và xác nhận thanh toán placeholder.
            </p>
          </div>
          <div className="rounded-lg bg-white p-5 shadow-sm">
            <PackageCheck className="h-6 w-6 text-amber-600" aria-hidden="true" />
            <p className="mt-3 text-sm font-semibold text-slate-950">
              Hospital commerce
            </p>
            <p className="mt-2 text-sm leading-6 text-slate-600">
              Sản phẩm phục hồi do nền tảng/bệnh viện quản lý.
            </p>
          </div>
        </div>
      </section>
    </>
  )
}
