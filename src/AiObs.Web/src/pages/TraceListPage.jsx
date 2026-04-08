import { useState, useEffect, useCallback } from 'react'
import { getTraces, deleteTrace, exportTrace, exportTraces } from '../api/tracesApi'
import FilterBar from '../components/FilterBar'
import TraceTable from '../components/TraceTable'
import ExportButton from '../components/ExportButton'

export default function TraceListPage() {
  const [traces, setTraces] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [filters, setFilters] = useState({ name: '', from: '', to: '', limit: 100, tags: {} })

  const loadTraces = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await getTraces(filters)
      setTraces(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }, [filters])

  useEffect(() => {
    loadTraces()
  }, [loadTraces])

  async function handleDelete(id) {
    try {
      await deleteTrace(id)
      await loadTraces()
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleExport(id) {
    try {
      await exportTrace(id)
    } catch (err) {
      setError(err.message)
    }
  }

  async function handleExportFiltered() {
    try {
      await exportTraces(filters)
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <div className="page">
      <h1 style={{ marginTop: 0 }}>Traces</h1>
      <FilterBar filters={filters} onChange={setFilters} />
      <div style={{ marginBottom: 12 }}>
        <ExportButton onClick={handleExportFiltered} label="Export filtered" />
      </div>
      {error && <div className="error">{error}</div>}
      {loading ? (
        <p>Loading…</p>
      ) : (
        <TraceTable traces={traces} onDelete={handleDelete} onExport={handleExport} />
      )}
    </div>
  )
}
