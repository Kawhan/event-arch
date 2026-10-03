# Build context: the repository root (see docker-compose.yml).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore first, copying only project files, so package downloads are cached
# by Docker and only re-run when a .csproj changes.
COPY src/EventArch.Contracts/*.csproj src/EventArch.Contracts/
COPY src/EventArch.Statement.Worker/*.csproj src/EventArch.Statement.Worker/
RUN dotnet restore src/EventArch.Statement.Worker/EventArch.Statement.Worker.csproj

COPY src/EventArch.Contracts/ src/EventArch.Contracts/
COPY src/EventArch.Statement.Worker/ src/EventArch.Statement.Worker/
RUN dotnet publish src/EventArch.Statement.Worker/EventArch.Statement.Worker.csproj -c Release -o /app --no-restore

# ---- runtime ----
# The aspnet image is required even without HTTP endpoints:
# Serilog.AspNetCore depends on the ASP.NET Core shared framework.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "EventArch.Statement.Worker.dll"]
