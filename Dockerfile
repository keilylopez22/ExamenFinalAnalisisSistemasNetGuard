# syntax=docker/dockerfile:1
# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY NetGuardGT.Api/NetGuardGT.Api.csproj NetGuardGT.Api/
RUN dotnet restore NetGuardGT.Api/NetGuardGT.Api.csproj --no-cache

COPY NetGuardGT.Api/ NetGuardGT.Api/
WORKDIR /src/NetGuardGT.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "NetGuardGT.Api.dll"]
