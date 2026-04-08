# ADR-009 — nginx reverse proxy for API routing

**Date:** 2026-04-08
**Status:** Accepted

## Context

The React frontend needs to communicate with the ASP.NET Core API. Two options were considered:

**A) Build-time API URL** — the API's IP address (`http://192.168.x.x:5000`) is baked into the Vite build via `VITE_API_URL`. Simple, but fragile: if the NAS IP changes, the frontend must be rebuilt and redeployed.

**B) nginx reverse proxy** — nginx serves the React static files and proxies `/api/*` requests to the API container on the internal Docker network. The frontend uses only relative URLs (`/api/traces`). No IP is embedded in the build.

## Decision

Option B — nginx reverse proxy.

## Reasons

- The NAS IP may change (DHCP lease renewal, network reconfiguration). Option B decouples the frontend build from the network topology.
- The nginx container becomes the single public entry point on port 8080. The API container is not exposed on the host network — it is only reachable within the Docker network by nginx. This reduces the attack surface even on a trusted local network.
- Relative URLs in the frontend mean the same build works in local development (via Vite's dev server proxy) and in production (via nginx), with no environment-specific build steps.
- nginx:alpine adds approximately 10 MB of memory overhead — negligible.

## nginx routing rule

```nginx
location /api/ {
    proxy_pass http://api:5000/;
}
```

`api` is the Docker Compose service name. DNS resolution within the Docker network handles the routing — no IP addresses anywhere in the configuration.

## Consequences

- The API is not directly accessible from the host network. Developers who want to call the API directly (e.g. with curl or Postman) must either go through nginx (`http://192.168.x.x:8080/api/traces`) or temporarily expose the API port in docker-compose.yml for debugging.
- The Vite dev server must be configured with a local proxy (`/api` → `http://localhost:5000`) to match production behaviour during development.
