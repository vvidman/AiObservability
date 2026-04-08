import { useState, useEffect } from 'react'

export default function FilterBar({ filters, onChange }) {
  const [tagEntries, setTagEntries] = useState(
    Object.entries(filters.tags || {}).map(([k, v]) => ({ key: k, value: v }))
  )

  useEffect(() => {
    setTagEntries(
      Object.entries(filters.tags || {}).map(([k, v]) => ({ key: k, value: v }))
    )
  }, [filters.tags])

  function updateTags(entries) {
    setTagEntries(entries)
    const tags = {}
    for (const { key, value } of entries) {
      if (key) tags[key] = value
    }
    onChange({ ...filters, tags })
  }

  function addTag() {
    updateTags([...tagEntries, { key: '', value: '' }])
  }

  function removeTag(index) {
    updateTags(tagEntries.filter((_, i) => i !== index))
  }

  function updateTag(index, field, val) {
    const updated = tagEntries.map((e, i) => i === index ? { ...e, [field]: val } : e)
    updateTags(updated)
  }

  return (
    <div style={{ background: '#fff', border: '1px solid #e0e0e0', borderRadius: 6, padding: 16, marginBottom: 16 }}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span>Name</span>
          <input
            type="text"
            placeholder="partial match"
            value={filters.name || ''}
            onChange={e => onChange({ ...filters, name: e.target.value })}
            style={{ width: 180 }}
          />
        </label>

        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span>From</span>
          <input
            type="datetime-local"
            value={filters.from || ''}
            onChange={e => onChange({ ...filters, from: e.target.value })}
          />
        </label>

        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span>To</span>
          <input
            type="datetime-local"
            value={filters.to || ''}
            onChange={e => onChange({ ...filters, to: e.target.value })}
          />
        </label>

        <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span>Limit</span>
          <input
            type="number"
            value={filters.limit || 100}
            min={1}
            style={{ width: 80 }}
            onChange={e => onChange({ ...filters, limit: parseInt(e.target.value, 10) || 100 })}
          />
        </label>
      </div>

      <div style={{ marginTop: 12 }}>
        <div style={{ fontWeight: 600, marginBottom: 6 }}>Tag filters</div>
        {tagEntries.map((entry, i) => (
          <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 6 }}>
            <input
              type="text"
              placeholder="key"
              value={entry.key}
              onChange={e => updateTag(i, 'key', e.target.value)}
              style={{ width: 120 }}
            />
            <input
              type="text"
              placeholder="value"
              value={entry.value}
              onChange={e => updateTag(i, 'value', e.target.value)}
              style={{ width: 180 }}
            />
            <button onClick={() => removeTag(i)}>Remove</button>
          </div>
        ))}
        <button onClick={addTag}>Add tag filter</button>
      </div>
    </div>
  )
}
