import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { getTrace, exportTrace } from '../api/tracesApi'
import SpanTree from '../components/SpanTree'
import ExportButton from '../components/ExportButton'

function formatDate(iso) {
  const d = new Date(iso)
  const pad = n => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

function formatDuration(trace) {
  const ms = new Date(trace.completedAt) - new Date(trace.startedAt)
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

export default function TraceDetailPage() {
  const { id } = useParams()
  const [trace, setTrace] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    getTrace(id)
      .then(setTrace)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [id])

  async function handleExport() {
    try {
      await exportTrace(id)
    } catch (err) {
      setError(err.message)
    }
  }

  if (loading) return <div className="page"><p>Loading…</p></div>
  if (error) return <div className="page"><div className="error">{error}</div></div>
  if (!trace) return null

  const status = trace.rootSpans?.some(s => s.status === 'Error') ? 'Error' : 'Ok'

  return (
    <div className="page">
      <div style={{ marginBottom: 16 }}>
        <Link to="/">← Back to traces</Link>
      </div>

      <div style={{ background: '#fff', border: '1px solid #e0e0e0', borderRadius: 6, padding: 16, marginBottom: 16 }}>
        <h2 style={{ marginTop: 0, marginBottom: 12 }}>{trace.name}</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: '6px 16px', fontSize: 13 }}>
          <strong>ID</strong><span style={{ fontFamily: 'monospace' }}>{trace.id}</span>
          <strong>Started at</strong><span>{formatDate(trace.startedAt)}</span>
          <strong>Duration</strong><span>{formatDuration(trace)}</span>
          <strong>Status</strong>
          <span className={status === 'Error' ? 'status-error' : 'status-ok'}>{status}</span>
          <strong>Tags</strong>
          <span>
            {Object.entries(trace.tags || {}).map(([k, v]) => (
              <span key={k} className="tag-pill">{k}={v}</span>
            ))}
          </span>
        </div>
      </div>

      <div style={{ marginBottom: 16 }}>
        <ExportButton onClick={handleExport} />
      </div>

      <div style={{ background: '#fff', border: '1px solid #e0e0e0', borderRadius: 6, padding: 16 }}>
        <h3 style={{ marginTop: 0 }}>Spans</h3>
        <SpanTree spans={trace.rootSpans} />
      </div>
    </div>
  )
}
