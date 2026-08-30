#!/usr/bin/env pwsh
# Stop local development infrastructure.
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
docker compose -f "$repoRoot/infra/docker/docker-compose.yml" down
