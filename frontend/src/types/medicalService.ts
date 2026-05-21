export interface MedicalService {
  id: string
  name: string
  description?: string | null
  durationMinutes: number
  price: number
  currency: string
  isActive: boolean
  noShowFeeEnabled: boolean
  noShowFeeAmount?: number | null
}
