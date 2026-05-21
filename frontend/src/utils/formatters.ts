export function formatCurrency(value: number, currency = 'VND'): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(value)
}

export function formatDateTime(value?: string | null): string {
  if (!value) {
    return 'Chưa có lịch'
  }

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatDate(value?: string | null): string {
  if (!value) {
    return 'Chưa cập nhật'
  }

  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
  }).format(new Date(value))
}

export function shortId(value?: string | null): string {
  if (!value) {
    return 'N/A'
  }

  return value.length > 8 ? value.slice(0, 8) : value
}
