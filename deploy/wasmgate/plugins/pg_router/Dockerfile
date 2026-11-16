FROM rust:1.83-slim AS build

RUN rustup target add wasm32-unknown-unknown

WORKDIR /src
COPY Cargo.toml ./
COPY src ./src
RUN cargo build --release --target wasm32-unknown-unknown

FROM scratch
COPY --from=build /src/target/wasm32-unknown-unknown/release/pg_router.wasm /pg_router.wasm
