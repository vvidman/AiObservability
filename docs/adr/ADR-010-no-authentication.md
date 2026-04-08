# ADR-010 — No authentication on the web UI and API

**Date:** 2026-04-08
**Status:** Accepted

## Context

The trace viewer exposes potentially sensitive data (LLM inputs, outputs, prompt content). Authentication was considered.

## Decision

No authentication is implemented. The UI and API are accessible to anyone on the local network without credentials.

## Reasons

- The NAS is on a trusted home/local network. External access is not exposed.
- The data (AI pipeline traces) is developer tooling data, not user PII or financial data.
- Adding authentication (even HTTP Basic Auth) adds implementation complexity, credential management overhead, and a credential storage decision — all disproportionate to the risk in this context.
- The nginx container is not exposed to the internet. Port 8080 is only reachable within the local network.

## Consequences

- Any device on the local network can view and delete traces. This is an accepted risk for a single-developer hobby project on a home network.
- If the threat model changes (e.g. the NAS becomes internet-accessible, or the project is shared with a team), HTTP Basic Auth or token-based auth should be added. The nginx configuration is the natural place to add Basic Auth with minimal code changes.
