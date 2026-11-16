# syntax=docker/dockerfile:1
#
# Builds the WasmGate data plane from a pinned upstream revision and packages
# the routing plugins plus this repository's WasmGate configuration.
#
# Override the revision for a demo of a newer WasmGate build:
#   docker compose build --build-arg WASMGATE_REF=main wasmgate

FROM golang:1.27-alpine AS build

ARG WASMGATE_REF=2f18328fbe8bce96d93516e2fdbbb63c25c6c152

RUN apk add --no-cache ca-certificates curl

WORKDIR /src
RUN curl -fsSL "https://github.com/Fridmak/WasmGate/archive/${WASMGATE_REF}.tar.gz" -o /tmp/wasmgate.tar.gz \
    && tar -xzf /tmp/wasmgate.tar.gz --strip-components=1 -C /src \
    && rm /tmp/wasmgate.tar.gz

RUN go mod download
RUN CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/wasmgate ./cmd/wasmgate

FROM alpine:3.22

RUN apk add --no-cache ca-certificates curl jq \
    && adduser -D -u 10001 wasmgate \
    && mkdir -p /data/agent \
    && chown -R wasmgate:wasmgate /data

WORKDIR /etc/wasmgate

COPY --from=build /out/wasmgate /usr/local/bin/wasmgate
COPY --from=build /src/plugins/examples/path_router/path_router.wasm /etc/wasmgate/path_router.wasm
COPY --chown=wasmgate:wasmgate deploy/wasmgate/pg_router.wasm /etc/wasmgate/pg_router.wasm
COPY --chown=wasmgate:wasmgate deploy/wasmgate/wasmgate.yaml /etc/wasmgate/wasmgate.yaml
COPY --chown=wasmgate:wasmgate deploy/wasmgate/entrypoint.sh /usr/local/bin/wasmgate-entrypoint.sh
RUN chmod 0755 /usr/local/bin/wasmgate-entrypoint.sh

USER wasmgate

EXPOSE 8080 5433 9099

ENTRYPOINT ["/usr/local/bin/wasmgate-entrypoint.sh"]
