import { useState } from 'react'
import SpanTree from './SpanTree'

function formatDuration(span) {
  const ms = new Date(span.completedAt) - new Date(span.startedAt)
  if (ms < 1000) return `${ms}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

export default function SpanNode({ span }) {
  const [expanded, setExpanded] = useState(span.status === 'Error')

  const hasContent =
    span.input != null ||
    span.output != null ||
    (span.metadata && Object.keys(span.metadata).length > 0) ||
    span.errorMessage ||
    (span.children && span.children.length > 0)

  return (
    <div style={{ margin: '4px 0' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        {hasContent && (
          <button
            onClick={() => setExpanded(e => !e)}
            style={{ width: 22, padding: 0, fontFamily: 'monospace', flexShrink: 0 }}
          >
            {expanded ? '−' : '+'}
          </button>
        )}
        {!hasContent && <span style={{ width: 22, display: 'inline-block' }} />}
        <span style={{ fontWeight: 500 }}>{span.name}</span>
        <span className={span.status === 'Error' ? 'status-error' : 'status-ok'} style={{ fontSize: 12 }}>
          {span.status}
        </span>
        <span style={{ color: '#666', fontSize: 12 }}>{formatDuration(span)}</span>
      </div>

      {expanded && hasContent && (
        <div style={{ paddingLeft: '1.5rem', marginTop: 4 }}>
          {span.input != null && (
            <div style={{ marginBottom: 6 }}>
              <div style={{ fontWeight: 600, fontSize: 12, color: '#555' }}>Input</div>
              <pre>{JSON.stringify(span.input, null, 2)}</pre>
            </div>
          )}

          {span.output != null && (
            <div style={{ marginBottom: 6 }}>
              <div style={{ fontWeight: 600, fontSize: 12, color: '#555' }}>Output</div>
              <pre>{JSON.stringify(span.output, null, 2)}</pre>
            </div>
          )}

          {span.metadata && Object.keys(span.metadata).length > 0 && (
            <div style={{ marginBottom: 6 }}>
              <div style={{ fontWeight: 600, fontSize: 12, color: '#555' }}>Metadata</div>
              <div style={{ fontSize: 12 }}>
                {Object.entries(span.metadata).map(([k, v]) => (
                  <div key={k} style={{ padding: '2px 0' }}>
                    <strong>{k}:</strong> {JSON.stringify(v)}
                  </div>
                ))}
              </div>
            </div>
          )}

          {span.errorMessage && (
            <div style={{ color: '#cc0000', marginBottom: 6, fontSize: 13 }}>
              {span.errorMessage}
            </div>
          )}

          {span.children && span.children.length > 0 && (
            <SpanTree spans={span.children} />
          )}
        </div>
      )}
    </div>
  )
}
