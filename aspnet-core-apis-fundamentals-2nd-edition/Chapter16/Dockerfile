# Build from the repository root, where global.json is; the pipeline passes the commit as SOURCE_REVISION:
#   docker build -f Chapter16/Dockerfile -t warehouse-api:local .
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS build
ARG SOURCE_REVISION=local
WORKDIR /src

# The SDK version rule and the project file first: this restore layer stays cached until they change
COPY global.json ./
COPY Chapter16/WarehouseApi/WarehouseApi.csproj Chapter16/WarehouseApi/
RUN dotnet restore Chapter16/WarehouseApi/WarehouseApi.csproj

COPY Chapter16/WarehouseApi/ Chapter16/WarehouseApi/
RUN dotnet publish Chapter16/WarehouseApi/WarehouseApi.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false -p:SourceRevisionId=$SOURCE_REVISION

# Chiseled: no shell and no package manager. The -extra variant adds the ICU and time zone data that
# Microsoft.Data.SqlClient needs; the plain chiseled image runs in invariant globalization mode, which it rejects
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WarehouseApi.dll"]
