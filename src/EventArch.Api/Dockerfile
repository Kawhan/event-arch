# Build context: the repository root (see docker-compose.yml).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first, copying only project files, so package downloads are cached
# by Docker and only re-run when a .csproj changes.
COPY src/EventArch.Domain/*.csproj src/EventArch.Domain/
COPY src/EventArch.Application/*.csproj src/EventArch.Application/
COPY src/EventArch.Contracts/*.csproj src/EventArch.Contracts/
COPY src/EventArch.Infrastructure/*.csproj src/EventArch.Infrastructure/
COPY src/EventArch.Api/*.csproj src/EventArch.Api/
RUN dotnet restore src/EventArch.Api/EventArch.Api.csproj

COPY src/ src/
RUN dotnet publish src/EventArch.Api/EventArch.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The official image already runs as the non-root "app" user and listens on 8080.
EXPOSE 8080
ENTRYPOINT ["dotnet", "EventArch.Api.dll"]
