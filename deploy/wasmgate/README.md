# WasmGate integration

This directory contains the WasmGate data-plane integration for the demo.

- `wasmgate.yaml` — HTTP/WS listeners, PostgreSQL routing and plugin config.
- `Dockerfile` — builds a static WasmGate binary from a pinned
  `Fridmak/WasmGate` revision and packages the config/plugins.
- `entrypoint.sh` — optional CP bootstrap/enrollment wrapper.
- `path_router.wasm` — reference plugin from WasmGate for `/api` vs `/ws`.
- `pg_router.wasm` — our plugin that routes PostgreSQL by database name.
- `plugins/pg_router/` — source and build instructions for `pg_router.wasm`.

Routing:

```
client :8080
  /api/* -> gateway:8080
  /ws*   -> notifications:8080

client :5433
  database=AuthDb          -> auth-db:5432
  database=TaskDb          -> task-db:5432
  database=NotificationsDb -> notifications-db:5432
```

The data plane is a managed agent of `wasmgate-cp`: it enrolls on first start,
keeps its identity in the `wasmgate_state` volume and applies signed YAML/artifact
releases. If the CP is unavailable, the last applied config keeps running.

`pg_router` parses the PostgreSQL StartupMessage. Clients must use
`sslmode=disable` / `SSL Mode=Disable`; an SSLRequest does not carry the database
name. `path_router` likewise inspects only the first client packet.

The pinned revision is `2f18328fbe8bce96d93516e2fdbbb63c25c6c152`, which includes
the `-validate` hotfix so configs with `admin_allow_remote` do not require a
runtime admin token during CP release validation.

The plugin and data plane are taken from the MIT-licensed
`https://github.com/Fridmak/WasmGate` repository.
