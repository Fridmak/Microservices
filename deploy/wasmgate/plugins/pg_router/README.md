# pg_router

WasmGate `on_data` plugin that routes PostgreSQL connections by the `database`
field of the StartupMessage.

Build the `.wasm` artifact:

```bash
docker build -t wasmgate-pg-router-build deploy/wasmgate/plugins/pg_router
docker create --name pg-router-build wasmgate-pg-router-build /pg_router.wasm
docker cp pg-router-build:/pg_router.wasm deploy/wasmgate/pg_router.wasm
docker rm pg-router-build
```

Routing:

| database | `route_pools` index |
|---|---|
| `AuthDb` | 2 |
| `TaskDb` | 3 |
| `NotificationsDb` | 4 |
| anything else | deny |

PostgreSQL clients must disable TLS for this listener (`SSL Mode=Disable` /
`sslmode=disable`), because the database name is not available in an
SSLRequest. L4 TLS passthrough still works for a client that connects directly
to a single database endpoint; it cannot be database-routed.
