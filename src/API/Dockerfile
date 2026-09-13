# syntax=docker/dockerfile:1

# --- restore & build -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached until a .csproj actually changes.
COPY src/API/API.csproj src/API/
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/API/API.csproj

COPY src/ src/
RUN dotnet publish src/API/API.csproj -c Release -o /app/publish --no-restore

# --- runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is not in the base image; it's needed for docker-compose's HTTP healthcheck against /health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# The base image's non-root "app" user (added in .NET 8+) can't bind ports < 1024,
# so the app listens on 8080 and that's what gets published/mapped.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
USER app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "API.dll"]
