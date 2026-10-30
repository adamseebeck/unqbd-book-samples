# Build stage: the .NET 11 SDK restores and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS build
WORKDIR /src

# Project files first, so the restore layer is reused until a package reference changes.
COPY ContosoSupportCopilot.Api/ContosoSupportCopilot.Api.csproj ContosoSupportCopilot.Api/
COPY ContosoSupportCopilot.Domain/ContosoSupportCopilot.Domain.csproj ContosoSupportCopilot.Domain/
COPY ContosoSupportCopilot.Infrastructure/ContosoSupportCopilot.Infrastructure.csproj ContosoSupportCopilot.Infrastructure/
RUN dotnet restore ContosoSupportCopilot.Api/ContosoSupportCopilot.Api.csproj

COPY ContosoSupportCopilot.Api/ ContosoSupportCopilot.Api/
COPY ContosoSupportCopilot.Domain/ ContosoSupportCopilot.Domain/
COPY ContosoSupportCopilot.Infrastructure/ ContosoSupportCopilot.Infrastructure/
RUN dotnet publish ContosoSupportCopilot.Api/ContosoSupportCopilot.Api.csproj -c Release --no-restore -o /out -p:UseAppHost=false

# The runtime image has no shell, so the build stage creates the folder for local SQLite files.
RUN mkdir /data

# Runtime stage: chiseled ASP.NET Core image, no shell or package manager, non-root user. The -extra
# variant adds ICU and time zone data: the plain chiseled image runs in globalization invariant mode,
# in which SqlClient cannot open a SQL Server connection.
FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra AS runtime
WORKDIR /app
COPY --from=build /out .
COPY --from=build --chown=$APP_UID /data /data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__SupportDb="Data Source=/data/contoso-support.db" \
    ConnectionStrings__CopilotDb="Data Source=/data/contoso-copilot.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContosoSupportCopilot.Api.dll"]
