# Chapter 18: build with the SDK image, run on the chiseled
# ASP.NET image (no shell, no package manager, a non-root user).
# The -extra variant keeps ICU, which the fr culture needs.
FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute AS build
WORKDIR /src
COPY . .
RUN dotnet tool restore \
    && dotnet publish AdventureWorks -c Release -o /app/store \
        -p:UseAppHost=false \
    && dotnet publish InventoryService -c Release \
        -o /app/inventory -p:UseAppHost=false \
    && dotnet ef migrations bundle -r linux-x64 \
        --project AdventureWorks.Infrastructure \
        --startup-project AdventureWorks -o /app/efbundle \
    && mkdir /data

FROM mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra
COPY --from=build /app /app
# The database folder belongs to the app's user (APP_UID, 1654).
COPY --from=build --chown=1654 /data /app/data
ENV ConnectionStrings__AdventureWorks=\
"Data Source=/app/data/adventureworks.db"
WORKDIR /app/store
# The image listens on 8080; EXPOSE only documents it.
EXPOSE 8080
USER $APP_UID
# The store by default. The warehouse: -w /app/inventory and the
# command InventoryService.dll; the migrations: /app/efbundle.
ENTRYPOINT ["dotnet"]
CMD ["AdventureWorks.dll"]
