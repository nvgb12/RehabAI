import type { FormEvent } from 'react'
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Stethoscope, Trash2 } from 'lucide-react'
import {
  createMedicalService,
  deleteMedicalService,
  getAdminMedicalServices,
  updateMedicalService,
} from '../api/admin'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { LoadingState } from '../components/LoadingState'
import { StatusBadge } from '../components/StatusBadge'
import { AdminLayout } from '../layouts/AdminLayout'
import type { MedicalService } from '../types/medicalService'
import { getApiErrorMessage } from '../utils/apiError'
import { formatCurrency } from '../utils/formatters'

interface ServiceFormState {
  name: string
  description: string
  durationMinutes: string
  price: string
  currency: string
  isActive: boolean
  noShowFeeEnabled: boolean
  noShowFeeAmount: string
}

const emptyServiceForm: ServiceFormState = {
  name: '',
  description: '',
  durationMinutes: '60',
  price: '0',
  currency: 'VND',
  isActive: true,
  noShowFeeEnabled: false,
  noShowFeeAmount: '',
}

export function AdminServicesPage() {
  const queryClient = useQueryClient()
  const [editingService, setEditingService] = useState<MedicalService | null>(
    null,
  )
  const [form, setForm] = useState<ServiceFormState>(emptyServiceForm)
  const [message, setMessage] = useState<string | null>(null)

  const servicesQuery = useQuery({
    queryKey: ['admin-medical-services'],
    queryFn: getAdminMedicalServices,
  })

  const saveMutation = useMutation({
    mutationFn: () => {
      const request = {
        name: form.name.trim(),
        description: form.description.trim() || null,
        durationMinutes: Number(form.durationMinutes),
        price: Number(form.price),
        currency: form.currency.trim() || 'VND',
        isActive: form.isActive,
        noShowFeeEnabled: form.noShowFeeEnabled,
        noShowFeeAmount: form.noShowFeeAmount
          ? Number(form.noShowFeeAmount)
          : null,
      }

      return editingService
        ? updateMedicalService(editingService.id, request)
        : createMedicalService(request)
    },
    onSuccess: (response) => {
      setMessage(response.message)
      setEditingService(null)
      setForm(emptyServiceForm)
      void queryClient.invalidateQueries({ queryKey: ['admin-medical-services'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteMedicalService,
    onSuccess: () => {
      setMessage('Medical service was soft-deleted.')
      void queryClient.invalidateQueries({ queryKey: ['admin-medical-services'] })
    },
  })

  function startEdit(service: MedicalService) {
    setEditingService(service)
    setForm({
      name: service.name,
      description: service.description ?? '',
      durationMinutes: service.durationMinutes.toString(),
      price: service.price.toString(),
      currency: service.currency || 'VND',
      isActive: service.isActive,
      noShowFeeEnabled: service.noShowFeeEnabled,
      noShowFeeAmount: service.noShowFeeAmount?.toString() ?? '',
    })
    setMessage(null)
  }

  function cancelEdit() {
    setEditingService(null)
    setForm(emptyServiceForm)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setMessage(null)
    saveMutation.mutate()
  }

  function handleDelete(service: MedicalService) {
    const confirmed = window.confirm(`Soft delete service "${service.name}"?`)
    if (confirmed) {
      deleteMutation.mutate(service.id)
    }
  }

  return (
    <AdminLayout
      title="Medical Services"
      description="Quản lý dịch vụ phục hồi chức năng dùng cho đặt lịch hẹn."
    >
      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-xl font-bold text-slate-950">
            {editingService ? 'Edit service' : 'Create service'}
          </h2>
          <p className="mt-2 text-sm text-slate-600">
            Danh sách hiện dùng public active service endpoint; service inactive
            hoặc đã xóa sẽ không hiện ở list này.
          </p>

          <form onSubmit={handleSubmit} className="mt-5 grid gap-4">
            <label className="grid gap-2">
              <span className="field-label">Name</span>
              <input
                className="field-input"
                value={form.name}
                onChange={(event) =>
                  setForm((value) => ({ ...value, name: event.target.value }))
                }
                required
              />
            </label>

            <label className="grid gap-2">
              <span className="field-label">Description</span>
              <textarea
                className="field-input min-h-24"
                value={form.description}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    description: event.target.value,
                  }))
                }
              />
            </label>

            <div className="grid gap-4 sm:grid-cols-3">
              <label className="grid gap-2">
                <span className="field-label">Duration</span>
                <input
                  className="field-input"
                  type="number"
                  min="1"
                  value={form.durationMinutes}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      durationMinutes: event.target.value,
                    }))
                  }
                  required
                />
              </label>
              <label className="grid gap-2">
                <span className="field-label">Price</span>
                <input
                  className="field-input"
                  type="number"
                  min="0"
                  value={form.price}
                  onChange={(event) =>
                    setForm((value) => ({ ...value, price: event.target.value }))
                  }
                  required
                />
              </label>
              <label className="grid gap-2">
                <span className="field-label">Currency</span>
                <input
                  className="field-input"
                  value={form.currency}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      currency: event.target.value,
                    }))
                  }
                />
              </label>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="inline-flex items-center gap-3 text-sm font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      isActive: event.target.checked,
                    }))
                  }
                />
                Active
              </label>
              <label className="inline-flex items-center gap-3 text-sm font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={form.noShowFeeEnabled}
                  onChange={(event) =>
                    setForm((value) => ({
                      ...value,
                      noShowFeeEnabled: event.target.checked,
                    }))
                  }
                />
                No-show fee
              </label>
            </div>

            <label className="grid gap-2">
              <span className="field-label">No-show fee amount</span>
              <input
                className="field-input"
                type="number"
                min="0"
                value={form.noShowFeeAmount}
                onChange={(event) =>
                  setForm((value) => ({
                    ...value,
                    noShowFeeAmount: event.target.value,
                  }))
                }
              />
            </label>

            {message ? (
              <div className="rounded-lg border border-rehab-200 bg-rehab-50 p-3 text-sm font-semibold text-rehab-800">
                {message}
              </div>
            ) : null}

            {saveMutation.isError ? (
              <ErrorState message={getApiErrorMessage(saveMutation.error)} />
            ) : null}

            <div className="flex flex-wrap gap-3">
              <button
                type="submit"
                className="btn-primary"
                disabled={saveMutation.isPending}
              >
                <Stethoscope className="h-4 w-4" aria-hidden="true" />
                {saveMutation.isPending
                  ? 'Saving'
                  : editingService
                    ? 'Update service'
                    : 'Create service'}
              </button>
              {editingService ? (
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={cancelEdit}
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </form>
        </section>

        <section>
          {servicesQuery.isLoading ? <LoadingState /> : null}

          {servicesQuery.isError ? (
            <ErrorState message={getApiErrorMessage(servicesQuery.error)} />
          ) : null}

          {servicesQuery.isSuccess && servicesQuery.data.length === 0 ? (
            <EmptyState
              icon={Stethoscope}
              title="No active services"
              message="Create a stroke rehabilitation service to make it available for booking."
            />
          ) : null}

          {servicesQuery.isSuccess && servicesQuery.data.length > 0 ? (
            <div className="grid gap-4">
              {servicesQuery.data.map((service) => (
                <article
                  key={service.id}
                  className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
                >
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <h3 className="text-xl font-bold text-slate-950">
                        {service.name}
                      </h3>
                      <p className="mt-2 text-sm leading-6 text-slate-600">
                        {service.description ?? 'No description'}
                      </p>
                    </div>
                    <StatusBadge value={service.isActive ? 'Active' : 'Inactive'} />
                  </div>

                  <div className="mt-4 grid gap-3 text-sm text-slate-600 sm:grid-cols-3">
                    <span>{service.durationMinutes} minutes</span>
                    <span>{formatCurrency(service.price, service.currency)}</span>
                    <span>
                      No-show:{' '}
                      {service.noShowFeeEnabled
                        ? formatCurrency(
                            service.noShowFeeAmount ?? 0,
                            service.currency,
                          )
                        : 'Off'}
                    </span>
                  </div>

                  <div className="mt-4 flex flex-wrap gap-3">
                    <button
                      type="button"
                      className="btn-secondary py-2"
                      onClick={() => startEdit(service)}
                    >
                      <Pencil className="h-4 w-4" aria-hidden="true" />
                      Edit
                    </button>
                    <button
                      type="button"
                      className="btn-secondary py-2 text-red-700 hover:border-red-200 hover:text-red-800"
                      onClick={() => handleDelete(service)}
                      disabled={deleteMutation.isPending}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                      Soft delete
                    </button>
                  </div>
                </article>
              ))}
            </div>
          ) : null}
        </section>
      </div>
    </AdminLayout>
  )
}
