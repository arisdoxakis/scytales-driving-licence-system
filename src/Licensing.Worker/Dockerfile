FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Licensing.Domain/Licensing.Domain.csproj src/Licensing.Domain/
COPY src/Licensing.Contracts/Licensing.Contracts.csproj src/Licensing.Contracts/
COPY src/Licensing.Infrastructure/Licensing.Infrastructure.csproj src/Licensing.Infrastructure/
COPY src/Licensing.ServiceDefaults/Licensing.ServiceDefaults.csproj src/Licensing.ServiceDefaults/
COPY src/Licensing.Worker/Licensing.Worker.csproj src/Licensing.Worker/
RUN dotnet restore src/Licensing.Worker/Licensing.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Licensing.Worker/Licensing.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /data/photos && chown $APP_UID /data/photos

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Licensing.Worker.dll"]