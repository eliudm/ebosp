#!/usr/bin/env pwsh
# Start local development infrastructure (Postgres, etc).
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
docker compose -f "$repoRoot/infra/docker/docker-compose.yml" up -d
docker compose -f "$repoRoot/infra/docker/docker-compose.yml" ps
