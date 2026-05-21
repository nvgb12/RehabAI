import { CalendarPlus, Stethoscope } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { Doctor } from '../types/doctor'
import { formatDateTime } from '../utils/formatters'

interface DoctorCardProps {
  doctor: Doctor
}

export function DoctorCard({ doctor }: DoctorCardProps) {
  return (
    <article className="flex h-full flex-col rounded-lg border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-soft">
      <div className="flex items-start gap-4">
        <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-rehab-50 text-rehab-700">
          {doctor.avatarUrl ? (
            <img
              src={doctor.avatarUrl}
              alt={doctor.fullName}
              className="h-full w-full object-cover"
            />
          ) : (
            <Stethoscope className="h-8 w-8" aria-hidden="true" />
          )}
        </div>
        <div>
          <h2 className="text-lg font-bold text-slate-950">
            {doctor.fullName}
          </h2>
          <p className="mt-1 text-sm font-semibold text-care-800">
            {doctor.specialtyName}
          </p>
        </div>
      </div>

      <p className="mt-4 line-clamp-3 text-sm leading-6 text-slate-600">
        {doctor.bio ??
          'Chuyên gia phục hồi chức năng đồng hành cùng bệnh nhân sau đột quỵ.'}
      </p>

      <div className="mt-5 rounded-lg border border-rehab-100 bg-rehab-50 p-4">
        <p className="text-xs font-semibold uppercase text-rehab-700">
          Lịch gần nhất
        </p>
        <p className="mt-1 text-sm font-bold text-slate-900">
          {formatDateTime(doctor.nextAvailableSlotStartTime)}
        </p>
      </div>

      <Link to={`/doctors/${doctor.doctorProfileId}`} className="btn-primary mt-5">
        <CalendarPlus className="h-4 w-4" aria-hidden="true" />
        Đặt lịch
      </Link>
    </article>
  )
}
