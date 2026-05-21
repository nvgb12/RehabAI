function pad2(value: number): string {
  return value.toString().padStart(2, '0')
}

export function toDateInputValue(date: Date): string {
  return `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`
}

export function getCurrentMonthRange() {
  const now = new Date()
  const start = new Date(now.getFullYear(), now.getMonth(), 1)
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0)

  return {
    fromDate: toDateInputValue(start),
    toDate: toDateInputValue(end),
  }
}

export function toStartOfDayOffset(date: string): string {
  return `${date}T00:00:00+07:00`
}

export function toEndOfDayOffset(date: string): string {
  return `${date}T23:59:59+07:00`
}
