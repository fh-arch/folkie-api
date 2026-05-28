#!/usr/bin/env bash
# ============================================================
# Folkie — Contabo VPS deploy helper
# ============================================================
# Run ON THE VPS, from /opt/folkie (or wherever you cloned both
# repos side-by-side: /opt/folkie/folkie_api + folkie_web).
#
# Prereqs:
#   - Docker + Docker Compose v2 installed
#   - DNS A record folkie.com.tr → VPS IP
#   - .env file present alongside this repo's docker-compose.prod.yml
# ============================================================
set -euo pipefail

cd "$(dirname "$0")/.."   # cd folkie_api

if [[ ! -f .env ]]; then
  echo "ERROR: .env not found. Copy .env.production.example to .env and fill secrets." >&2
  exit 1
fi

if [[ ! -d ../folkie_web ]]; then
  echo "ERROR: ../folkie_web not found. Clone folkie_web next to folkie_api." >&2
  exit 1
fi

echo "==> Pulling latest code"
git pull --ff-only
(cd ../folkie_web && git pull --ff-only)

echo "==> Building images"
docker compose -f docker-compose.prod.yml build --pull

echo "==> Applying EF Core migrations"
# Migrations run automatically on Folkie.Api boot via DbInitializer.
# If you prefer manual: uncomment the next line.
# docker compose -f docker-compose.prod.yml run --rm folkie-api dotnet ef database update

echo "==> Starting stack"
docker compose -f docker-compose.prod.yml up -d

echo "==> Pruning dangling images"
docker image prune -f

echo "==> Status"
docker compose -f docker-compose.prod.yml ps

echo
echo "Done. Tail logs with:"
echo "  docker compose -f docker-compose.prod.yml logs -f folkie-api folkie-web caddy"
