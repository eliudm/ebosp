# Build context: repo root (needs sibling projects for the solution's ProjectReferences).
# docker build -f infra/docker/api.Dockerfile -t ebosp-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/EBOSP.Domain/EBOSP.Domain.csproj src/EBOSP.Domain/
COPY src/EBOSP.Contracts/EBOSP.Contracts.csproj src/EBOSP.Contracts/
COPY src/EBOSP.Application/EBOSP.Application.csproj src/EBOSP.Application/
COPY src/EBOSP.Infrastructure/EBOSP.Infrastructure.csproj src/EBOSP.Infrastructure/
COPY src/EBOSP.Api/EBOSP.Api.csproj src/EBOSP.Api/
RUN dotnet restore src/EBOSP.Api/EBOSP.Api.csproj

COPY src/EBOSP.Domain/ src/EBOSP.Domain/
COPY src/EBOSP.Contracts/ src/EBOSP.Contracts/
COPY src/EBOSP.Application/ src/EBOSP.Application/
COPY src/EBOSP.Infrastructure/ src/EBOSP.Infrastructure/
COPY src/EBOSP.Api/ src/EBOSP.Api/
RUN dotnet publish src/EBOSP.Api/EBOSP.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Pull latest Alpine package patches (the base image tag can lag a few days behind the distro's
# security fixes) so the container-scan gate isn't failing on CVEs already fixed upstream.
RUN apk update && apk upgrade --no-cache

# Npgsql probes for GSSAPI (Kerberos) support at connect time; without krb5-libs it logs a
# scary-looking (but harmless, since local/dev auth doesn't use Kerberos) load failure.
RUN apk add --no-cache krb5-libs

# mcr.microsoft.com/dotnet/aspnet images ship a non-root "app" user (UID 64198) since .NET 8.
USER app

COPY --from=build --chown=app:app /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EBOSP.Api.dll"]
