#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")/.."

new_secret() {
    openssl rand -hex 32
}

ensure() {
    key="$1"
    value="$2"
    grep -q "^$key=" .env || printf '%s=%s\n' "$key" "$value" >> .env
}

if [ ! -f .env ]; then
    if docker volume ls --quiet --filter name=microservices_auth_pgdata | grep -q .; then
        echo "Existing Postgres volume found without .env. Restore .env or run 'docker compose down -v' first." >&2
        exit 1
    fi
    umask 077
    {
        echo "POSTGRES_PASSWORD=$(new_secret)"
        echo "JWT_KEY=$(new_secret)"
        echo "SERVICE_API_KEY=$(new_secret)"
        echo "FRONTEND_ORIGIN=http://localhost:5173"
        echo "WASMGATE_ADMIN_TOKEN=$(new_secret)"
        echo "WASMGATE_CP_ADMIN_TOKEN=$(new_secret)"
    } > .env
    echo "Created .env with generated secrets."
else
    ensure POSTGRES_PASSWORD "$(new_secret)"
    ensure JWT_KEY "$(new_secret)"
    ensure SERVICE_API_KEY "$(new_secret)"
    ensure FRONTEND_ORIGIN "http://localhost:5173"
    ensure WASMGATE_ADMIN_TOKEN "$(new_secret)"
    ensure WASMGATE_CP_ADMIN_TOKEN "$(new_secret)"
    echo "Using existing .env (missing values were added)."
fi

docker compose up -d --build "$@"

echo
echo "Microservices stack is up:"
echo "  Public API / SignalR : http://localhost:8080"
echo "  Swagger              : http://localhost:7001/swagger, :7002/swagger, :7003/swagger"
echo "  WasmGate admin       : http://localhost:19099 (Bearer token from .env)"
echo "  WasmGate CP UI       : http://localhost:19097 (Bearer token from .env)"
echo "  PostgreSQL via edge  : localhost:5433 (AuthDb/TaskDb/NotificationsDb)"
