# syntax=docker/dockerfile:1
#
# Builds the WasmGate data plane from a pinned upstream revision and packages
# the path_router plugin plus this repository's WasmGate configuration.
#
# Override the revision for a demo of a newer WasmGate build:
#   docker compose build --build-arg WASMGATE_REF=main wasmgate

FROM golang:1.27-alpine AS build

ARG WASMGATE_REF=4d9448fd23dbe30a6ea58e392e7bad3fe8307a39

RUN apk add --no-cache ca-certificates curl

WORKDIR /src
RUN curl -fsSL "https://github.com/Fridmak/WasmGate/archive/${WASMGATE_REF}.tar.gz" -o /tmp/wasmgate.tar.gz \
    && tar -xzf /tmp/wasmgate.tar.gz --strip-components=1 -C /src \
    && rm /tmp/wasmgate.tar.gz

RUN go mod download
RUN CGO_ENABLED=0 go build -trimpath -ldflags="-s -w" -o /out/wasmgate ./cmd/wasmgate

FROM alpine:3.22

RUN apk add --no-cache ca-certificates curl \
    && adduser -D -u 10001 wasmgate

WORKDIR /etc/wasmgate

COPY --from=build /out/wasmgate /usr/local/bin/wasmgate
# Keep the plugin from the same revision as the data plane. The local copy under
# deploy/wasmgate/ exists only for `wasmgate -validate` on the host.
COPY --from=build /src/plugins/examples/path_router/path_router.wasm /etc/wasmgate/path_router.wasm
COPY --chown=wasmgate:wasmgate deploy/wasmgate/wasmgate.yaml /etc/wasmgate/wasmgate.yaml

USER wasmgate

EXPOSE 8080 9099

ENTRYPOINT ["wasmgate", "-config", "/etc/wasmgate/wasmgate.yaml"]
