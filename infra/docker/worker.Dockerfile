# Build context: repo root (needs sibling projects for the solution's ProjectReferences).
# docker build -f infra/docker/worker.Dockerfile -t ebosp-worker .
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/EBOSP.Domain/EBOSP.Domain.csproj src/EBOSP.Domain/
COPY src/EBOSP.Contracts/EBOSP.Contracts.csproj src/EBOSP.Contracts/
COPY src/EBOSP.Application/EBOSP.Application.csproj src/EBOSP.Application/
COPY src/EBOSP.Infrastructure/EBOSP.Infrastructure.csproj src/EBOSP.Infrastructure/
COPY src/EBOSP.Worker/EBOSP.Worker.csproj src/EBOSP.Worker/
RUN dotnet restore src/EBOSP.Worker/EBOSP.Worker.csproj

COPY src/EBOSP.Domain/ src/EBOSP.Domain/
COPY src/EBOSP.Contracts/ src/EBOSP.Contracts/
COPY src/EBOSP.Application/ src/EBOSP.Application/
COPY src/EBOSP.Infrastructure/ src/EBOSP.Infrastructure/
COPY src/EBOSP.Worker/ src/EBOSP.Worker/
RUN dotnet publish src/EBOSP.Worker/EBOSP.Worker.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Pull latest Alpine package patches (the base image tag can lag a few days behind the distro's
# security fixes) so the container-scan gate isn't failing on CVEs already fixed upstream.
RUN apk update && apk upgrade --no-cache

# mcr.microsoft.com/dotnet/aspnet images ship a non-root "app" user (UID 64198) since .NET 8.
USER app

ENV HEARTBEAT_FILE_PATH=/tmp/worker-healthy
COPY --from=build --chown=app:app /app .

HEALTHCHECK --interval=15s --timeout=5s --start-period=15s --retries=3 \
    CMD sh -c 'test -f "$HEARTBEAT_FILE_PATH" && [ $(( $(date +%s) - $(cat "$HEARTBEAT_FILE_PATH") )) -lt 30 ]'

ENTRYPOINT ["dotnet", "EBOSP.Worker.dll"]
