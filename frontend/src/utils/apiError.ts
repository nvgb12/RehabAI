import axios from 'axios'

interface ProblemDetails {
  message?: string
  title?: string
  detail?: string
}

export function getApiErrorMessage(
  error: unknown,
  fallback = 'Có lỗi xảy ra. Vui lòng thử lại.',
): string {
  if (!axios.isAxiosError(error)) {
    return fallback
  }

  const data = error.response?.data as ProblemDetails | string | undefined
  if (typeof data === 'string') {
    return data
  }

  return data?.message ?? data?.detail ?? data?.title ?? fallback
}
