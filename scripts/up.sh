#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
    if docker volume inspect microservices_pgdata >/dev/null 2>&1; then
        echo "Existing Postgres volume found without .env. Restore .env or run 'docker compose down -v' first." >&2
        exit 1
    fi
    umask 077
    {
        echo "POSTGRES_PASSWORD=$(openssl rand -hex 32)"
        echo "JWT_KEY=$(openssl rand -hex 32)"
        echo "SERVICE_API_KEY=$(openssl rand -hex 32)"
        echo "FRONTEND_ORIGIN=http://localhost:5173"
        echo "WASMGATE_ADMIN_TOKEN=$(openssl rand -hex 32)"
    } > .env
    echo "Created .env with generated secrets."
else
    echo "Using existing .env."
fi

docker compose up -d --build "$@"

echo
echo "Microservices stack is up:"
echo "  Public API / SignalR : http://localhost:8080"
echo "  Swagger              : http://localhost:7001/swagger, :7002/swagger, :7003/swagger"
echo "  WasmGate admin       : http://localhost:19099 (Bearer token from .env)"
