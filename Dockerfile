FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore separat, så pakke-laget kun bygges om når csproj ændres.
COPY api/api.csproj api/
RUN dotnet restore api/api.csproj

COPY api/ api/
RUN dotnet publish api/api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# app_dbs/ og app_files/ oprettes i imaget med app-brugeren som ejer, så Docker-volumes
# arver ejerskabet første gang de oprettes (containeren kører ikke som root).
RUN mkdir -p /app/app_dbs /app/app_files && chown -R $APP_UID:$APP_UID /app/app_dbs /app/app_files

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "api.dll"]
