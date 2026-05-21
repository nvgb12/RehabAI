import { Activity, Mail, MapPin, Phone } from 'lucide-react'

export function AppFooter() {
  return (
    <footer className="border-t border-slate-100 bg-slate-950 text-white">
      <div className="page-container py-10">
        <div className="grid gap-8 md:grid-cols-[1.2fr_1fr_1fr]">
          <div>
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-rehab-500">
                <Activity className="h-5 w-5" aria-hidden="true" />
              </span>
              <span className="text-lg font-bold">RehabAI</span>
            </div>
            <p className="mt-4 max-w-md text-sm leading-6 text-slate-300">
              Nền tảng hỗ trợ phục hồi sau đột quỵ với đặt lịch chuyên gia,
              sản phẩm chăm sóc và luồng quản trị bệnh viện.
            </p>
          </div>

          <div>
            <h2 className="text-sm font-semibold text-white">Liên hệ</h2>
            <div className="mt-4 space-y-3 text-sm text-slate-300">
              <p className="flex items-center gap-2">
                <Phone className="h-4 w-4" aria-hidden="true" />
                1900 0000
              </p>
              <p className="flex items-center gap-2">
                <Mail className="h-4 w-4" aria-hidden="true" />
                care@rehabai.local
              </p>
              <p className="flex items-center gap-2">
                <MapPin className="h-4 w-4" aria-hidden="true" />
                Stroke rehab center
              </p>
            </div>
          </div>

          <div>
            <h2 className="text-sm font-semibold text-white">Web flow MVP</h2>
            <p className="mt-4 text-sm leading-6 text-slate-300">
              API backend dùng JWT, SQL Server và các placeholder thanh toán để
              kiểm thử trước khi tích hợp payment gateway thật.
            </p>
          </div>
        </div>
      </div>
    </footer>
  )
}
