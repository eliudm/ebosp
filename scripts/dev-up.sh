#!/usr/bin/env bash
# Start local development infrastructure (Postgres, etc).
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
docker compose -f "$repo_root/infra/docker/docker-compose.yml" up -d
docker compose -f "$repo_root/infra/docker/docker-compose.yml" ps
