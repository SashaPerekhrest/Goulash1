#!/usr/bin/env sh
set -eu

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
REGISTRY="${GHCR_REGISTRY:-ghcr.io}"
GHCR_USERNAME="${GHCR_USERNAME:-${GHCR_OWNER:-}}"

: "${GHCR_OWNER:?GHCR_OWNER is required}"
: "${IMAGE_TAG:?IMAGE_TAG is required}"

export GHCR_OWNER
export IMAGE_TAG

if [ -n "${GHCR_TOKEN:-}" ]; then
  printf '%s' "$GHCR_TOKEN" | docker login "$REGISTRY" -u "$GHCR_USERNAME" --password-stdin
fi

docker compose -f "$COMPOSE_FILE" pull
docker compose -f "$COMPOSE_FILE" down --remove-orphans
docker compose -f "$COMPOSE_FILE" up -d

docker image prune -af
docker builder prune -af || true

docker compose -f "$COMPOSE_FILE" ps
