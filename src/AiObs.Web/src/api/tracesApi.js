function buildQueryString(filters) {
  const params = new URLSearchParams()

  if (filters.name) params.set('name', filters.name)
  if (filters.from) params.set('from', filters.from)
  if (filters.to) params.set('to', filters.to)
  if (filters.limit) params.set('limit', String(filters.limit))

  if (filters.tags) {
    for (const [key, value] of Object.entries(filters.tags)) {
      if (key && value) params.set(`tag_${key}`, value)
    }
  }

  const qs = params.toString()
  return qs ? `?${qs}` : ''
}

function getFilenameFromResponse(response, fallback) {
  const disposition = response.headers.get('Content-Disposition')
  if (disposition) {
    const match = disposition.match(/filename=([^;]+)/)
    if (match) return match[1].trim()
  }
  return fallback
}

function triggerDownload(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

export async function getTraces(filters = {}) {
  const qs = buildQueryString(filters)
  const response = await fetch(`/api/traces${qs}`)
  if (!response.ok) throw new Error(`Failed to fetch traces: ${response.status}`)
  return response.json()
}

export async function getTrace(id) {
  const response = await fetch(`/api/traces/${id}`)
  if (!response.ok) throw new Error(`Failed to fetch trace: ${response.status}`)
  return response.json()
}

export async function deleteTrace(id) {
  const response = await fetch(`/api/traces/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error(`Failed to delete trace: ${response.status}`)
  return true
}

export async function exportTrace(id) {
  const response = await fetch(`/api/traces/${id}/export`)
  if (!response.ok) throw new Error(`Failed to export trace: ${response.status}`)
  const blob = await response.blob()
  const filename = getFilenameFromResponse(response, `${id}.json`)
  triggerDownload(blob, filename)
}

export async function exportTraces(filters = {}) {
  const qs = buildQueryString(filters)
  const response = await fetch(`/api/traces/export${qs}`)
  if (!response.ok) throw new Error(`Failed to export traces: ${response.status}`)
  const blob = await response.blob()
  const filename = getFilenameFromResponse(response, 'traces-export.json')
  triggerDownload(blob, filename)
}
