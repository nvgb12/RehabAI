import { useQuery } from '@tanstack/react-query'
import { Stethoscope } from 'lucide-react'
import { useState } from 'react'
import { getDoctors } from '../api/doctors'
import { DoctorCard } from '../components/DoctorCard'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { SearchBar } from '../components/SearchBar'
import { getApiErrorMessage } from '../utils/apiError'

export function DoctorListPage() {
  const [keyword, setKeyword] = useState('')
  const doctorsQuery = useQuery({
    queryKey: ['doctors', keyword],
    queryFn: () => getDoctors({ keyword: keyword || undefined }),
  })

  return (
    <section className="bg-white py-10 sm:py-14">
      <div className="page-container">
        <div className="grid gap-5 md:grid-cols-[1fr_420px] md:items-end">
          <div>
            <h1 className="text-3xl font-bold text-slate-950 sm:text-4xl">
              Bác sĩ phục hồi chức năng
            </h1>
            <p className="mt-3 max-w-2xl text-base leading-7 text-slate-600">
              Danh sách chỉ hiển thị bác sĩ Active và đã được Admin duyệt
              public profile. Lịch trống chỉ ảnh hưởng đặt lịch trực tiếp.
            </p>
          </div>
          <SearchBar
            value={keyword}
            onChange={setKeyword}
            placeholder="Tìm theo tên bác sĩ hoặc mô tả chuyên môn..."
          />
        </div>

        <div className="mt-8">
          {doctorsQuery.isLoading ? <LoadingState /> : null}

          {doctorsQuery.isError ? (
            <ErrorState message={getApiErrorMessage(doctorsQuery.error)} />
          ) : null}

          {doctorsQuery.isSuccess && doctorsQuery.data.length === 0 ? (
            <EmptyState
              icon={Stethoscope}
              title="Chưa có bác sĩ phù hợp"
              message="Bác sĩ cần Active và public profile đã được Admin duyệt."
            />
          ) : null}

          {doctorsQuery.isSuccess && doctorsQuery.data.length > 0 ? (
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              {doctorsQuery.data.map((doctor) => (
                <DoctorCard key={doctor.doctorProfileId} doctor={doctor} />
              ))}
            </div>
          ) : null}
        </div>
      </div>
    </section>
  )
}
