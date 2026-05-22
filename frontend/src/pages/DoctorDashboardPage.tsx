import { useQuery } from '@tanstack/react-query'
import {
  CalendarClock,
  CalendarDays,
  CheckCircle2,
  ClipboardList,
  UserRoundCog,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { getMyDoctorDashboard } from '../api/doctors'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatCard } from '../components/StatCard'
import { StatusBadge } from '../components/StatusBadge'
import { DoctorLayout } from '../layouts/DoctorLayout'
import { getApiErrorMessage } from '../utils/apiError'
import { formatDateTime } from '../utils/formatters'

const quickLinks = [
  {
    to: '/doctor/profile',
    label: 'Profile',
    description: 'Review public profile, phone, bio, and avatar.',
    icon: UserRoundCog,
  },
  {
    to: '/doctor/schedule',
    label: 'Schedule',
    description: 'Create and manage available consultation slots.',
    icon: CalendarDays,
  },
  {
    to: '/doctor/appointments',
    label: 'Appointments',
    description: 'View patient appointments assigned to your profile.',
    icon: ClipboardList,
  },
]

export function DoctorDashboardPage() {
  const dashboardQuery = useQuery({
    queryKey: ['doctor-dashboard'],
    queryFn: getMyDoctorDashboard,
  })

  const dashboard = dashboardQuery.data

  return (
    <DoctorLayout
      title="Doctor Dashboard"
      description="Workspace for stroke rehabilitation appointments, availability, and profile readiness."
    >
      {dashboardQuery.isLoading ? <LoadingState /> : null}

      {dashboardQuery.isError ? (
        <ErrorState message={getApiErrorMessage(dashboardQuery.error)} />
      ) : null}

      {dashboard ? (
        <div className="space-y-6">
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
            <StatCard
              label="Upcoming"
              value={dashboard.upcomingAppointmentCount.toString()}
              description="Future appointments not cancelled or expired."
              icon={CalendarClock}
            />
            <StatCard
              label="Today"
              value={dashboard.todayAppointmentCount.toString()}
              description="Appointments scheduled for the current day."
              icon={ClipboardList}
            />
            <StatCard
              label="Available slots"
              value={dashboard.availableSlotCount.toString()}
              description="Future slots ready for patient booking."
              icon={CalendarDays}
            />
            <StatCard
              label="Booked slots"
              value={dashboard.bookedSlotCount.toString()}
              description="Future slots already booked."
              icon={CheckCircle2}
            />
          </div>

          <div className="grid gap-5 lg:grid-cols-[1fr_1.2fr]">
            <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <h2 className="text-xl font-bold text-slate-950">
                    Profile status
                  </h2>
                  <p className="mt-2 text-sm text-slate-600">
                    {dashboard.fullName}
                  </p>
                </div>
                <StatusBadge
                  value={
                    dashboard.publicProfileApproved
                      ? 'ProfileApproved'
                      : 'PendingApproval'
                  }
                />
              </div>
              <p className="mt-5 text-sm leading-6 text-slate-600">
                Public listing requires an Active Doctor account, approved profile,
                and at least one future Available schedule slot.
              </p>
            </section>

            <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="text-xl font-bold text-slate-950">
                Next appointment
              </h2>
              {dashboard.nextAppointment ? (
                <div className="mt-4 rounded-lg bg-slate-50 p-4">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <p className="font-bold text-slate-950">
                        {dashboard.nextAppointment.patientName}
                      </p>
                      <p className="mt-1 text-sm text-slate-600">
                        {dashboard.nextAppointment.medicalServiceName}
                      </p>
                      <p className="mt-2 text-sm font-semibold text-care-800">
                        {formatDateTime(dashboard.nextAppointment.startTime)}
                      </p>
                    </div>
                    <StatusBadge value={dashboard.nextAppointment.status} />
                  </div>
                </div>
              ) : (
                <p className="mt-4 text-sm text-slate-600">
                  No upcoming appointment has been assigned yet.
                </p>
              )}
            </section>
          </div>

          <div className="grid gap-5 md:grid-cols-3">
            {quickLinks.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className="group rounded-lg border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-care-300 hover:shadow-soft"
              >
                <span className="flex h-11 w-11 items-center justify-center rounded-lg bg-care-50 text-care-700">
                  <item.icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <h2 className="mt-4 text-lg font-bold text-slate-950">
                  {item.label}
                </h2>
                <p className="mt-2 text-sm leading-6 text-slate-600">
                  {item.description}
                </p>
                <p className="mt-4 text-sm font-bold text-care-800">
                  Open
                  <span className="ml-1 transition group-hover:ml-2">-&gt;</span>
                </p>
              </Link>
            ))}
          </div>
        </div>
      ) : null}
    </DoctorLayout>
  )
}
