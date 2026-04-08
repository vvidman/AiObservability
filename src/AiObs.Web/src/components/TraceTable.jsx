import { useNavigate } from 'react-router-dom'

function formatDate(iso) {
  const d = new Date(iso)
  const pad = n => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

function formatDuration(ms) {
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

export default function TraceTable({ traces, onDelete, onExport }) {
  const navigate = useNavigate()

  if (!traces.length) {
    return <p>No traces found.</p>
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Started at</th>
          <th>Duration</th>
          <th>Status</th>
          <th>Tags</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {traces.map(trace => (
          <tr key={trace.id}>
            <td>{trace.name}</td>
            <td>{formatDate(trace.startedAt)}</td>
            <td>{formatDuration(trace.durationMs)}</td>
            <td className={trace.status === 'Error' ? 'status-error' : 'status-ok'}>{trace.status}</td>
            <td>
              {Object.entries(trace.tags || {}).map(([k, v]) => (
                <span key={k} className="tag-pill">{k}={v}</span>
              ))}
            </td>
            <td>
              <button onClick={() => navigate(`/traces/${trace.id}`)}>View</button>{' '}
              <button
                onClick={() => {
                  if (window.confirm(`Delete trace "${trace.name}"?`)) {
                    onDelete(trace.id)
                  }
                }}
              >
                Delete
              </button>{' '}
              <button onClick={() => onExport(trace.id)}>Export</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
