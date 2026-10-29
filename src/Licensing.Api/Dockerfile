FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer is cached until a dependency changes
COPY src/Licensing.Domain/Licensing.Domain.csproj src/Licensing.Domain/
COPY src/Licensing.Contracts/Licensing.Contracts.csproj src/Licensing.Contracts/
COPY src/Licensing.Infrastructure/Licensing.Infrastructure.csproj src/Licensing.Infrastructure/
COPY src/Licensing.ServiceDefaults/Licensing.ServiceDefaults.csproj src/Licensing.ServiceDefaults/
COPY src/Licensing.Api/Licensing.Api.csproj src/Licensing.Api/
RUN dotnet restore src/Licensing.Api/Licensing.Api.csproj

COPY src/ src/
RUN dotnet publish src/Licensing.Api/Licensing.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl for the Compose healthcheck; the photos folder owned by the non-root app user
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/photos && chown $APP_UID /data/photos

COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Licensing.Api.dll"]