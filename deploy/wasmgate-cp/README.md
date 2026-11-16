# WasmGate control plane

This directory integrates the optional WasmGate control plane:

- `Dockerfile` — builds the control plane and its `wasmgate -validate` helper
  from a pinned `Fridmak/WasmGate` revision.
- `migrations/` — PostgreSQL schema migrations copied from that revision.

The control plane is not on the traffic path. It stores YAML configuration
versions, artifacts and releases, and enrolls data planes over mTLS. If it is
down, the data plane continues serving its last applied configuration.

The compose setup bootstraps one data-plane agent automatically for the demo.
The data plane receives a registration token from the CP admin API on first
start; this is a development convenience, not a production credential model.

The pinned upstream revision (`2f18328`) includes the `-validate` hotfix: configs
with `admin_allow_remote` no longer require `WASMGATE_ADMIN_TOKEN` during release
validation. Do not add the data-plane admin token to the CP container as a
workaround.

Control plane UI: `http://localhost:19097` (Bearer admin token from `.env`).

Production note: pre-provision an agent/enrollment token and give the data
plane only that token, not the CP admin token.
