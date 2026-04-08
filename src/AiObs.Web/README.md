# AiObs.Web

React frontend for browsing, filtering, and inspecting AI pipeline traces.

Built with Vite + React. Served via nginx in Docker.
All API calls are routed through the nginx reverse proxy — no hardcoded API URLs in the frontend.

---

## What lives here

```
AiObs.Web/
├── src/
│   ├── main.jsx
│   ├── App.jsx
│   ├── pages/
│   │   ├── TraceListPage.jsx     # Main list view with filters
│   │   └── TraceDetailPage.jsx   # Full trace with span tree
│   ├── components/
│   │   ├── TraceTable.jsx        # Sortable trace list
│   │   ├── FilterBar.jsx         # Name, date range, tag filters
│   │   ├── SpanTree.jsx          # Recursive span tree renderer
│   │   ├── SpanNode.jsx          # Single span with expand/collapse
│   │   └── ExportButton.jsx      # Triggers export download
│   └── api/
│       └── tracesApi.js          # Fetch wrappers for all endpoints
├── nginx.conf                    # Reverse proxy config
├── index.html
├── vite.config.js
└── package.json
```

---

## Pages

### Trace list (`/`)

- Filter bar: trace name (partial), date range (from/to), tag key-value pairs
- Sortable table: name, started at, duration, status (Ok/Error), tags
- Per-row actions: View detail, Delete, Export JSON
- Bulk export: exports all currently filtered traces as a single JSON file

### Trace detail (`/traces/:id`)

- Trace metadata: id, name, tags, total duration
- Span tree: recursive expand/collapse tree
- Each span shows: name, status, duration, input, output, metadata, error message
- Export button: downloads the full trace as JSON

---

## API communication

All requests use relative URLs (`/api/traces`, `/api/traces/:id`, etc.).
nginx proxies `/api/*` to the `api` container on the internal Docker network.

No environment variables, no hardcoded IPs in the frontend build.

---

## Running locally (development)

```bash
cd src/AiObs.Web
npm install
npm run dev
```

In development, configure the Vite proxy in `vite.config.js` to forward `/api` to the running API:

```js
// vite.config.js
export default {
  server: {
    proxy: {
      '/api': 'http://localhost:5000'
    }
  }
}
```

---

## Docker

Built via `docker/Dockerfile.web`. Multi-stage build:

1. **Build stage** (`node:20-alpine`): runs `npm run build`, produces `dist/`
2. **Serve stage** (`nginx:alpine`): serves `dist/` as static files, applies `nginx.conf` for API proxying

The nginx container is the only publicly exposed port (`8080`).
