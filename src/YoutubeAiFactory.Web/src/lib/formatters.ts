export function formatNumber(value: number | null) {
  return value === null
    ? "Unavailable"
    : new Intl.NumberFormat(undefined, { notation: "compact" }).format(value)
}

export function formatDate(value: string | null) {
  return value
    ? new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value))
    : "Unavailable"
}
