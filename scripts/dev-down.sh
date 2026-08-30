#!/usr/bin/env bash
# Stop local development infrastructure.
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
docker compose -f "$repo_root/infra/docker/docker-compose.yml" down
