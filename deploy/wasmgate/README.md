# WasmGate edge

This directory contains the WasmGate data-plane integration for the demo.

- `wasmgate.yaml` — listener/pools and `path_router` configuration.
- `Dockerfile` — builds a static WasmGate binary from a pinned
  `Fridmak/WasmGate` revision and packages the config.
- `path_router.wasm` — local copy of the reference plugin, used for
  `wasmgate -config ... -validate` on the host. The Docker image builds the
  plugin from the same pinned revision as the data plane.

Routing:

```
client :8080
  /api/* -> gateway:8080
  /ws*   -> notifications:8080

client :5433 (optional L4 demo)
  raw PostgreSQL -> postgres:5432
```

The PostgreSQL listener is intentionally separate from the application's
database path: services keep using `postgres:5432` directly.

The `path_router` plugin inspects the first client packet. It is suitable for
this local demo; for production prefer TLS termination plus host/SNI routing
(`api.*` / `ws.*`), or proxy the hub through YARP.

The plugin and data plane are taken from the MIT-licensed
`https://github.com/Fridmak/WasmGate` repository.
