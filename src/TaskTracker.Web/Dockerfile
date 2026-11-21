# Build from the repository root: docker build -f src/TaskTracker.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG DOTNET_TFM=net10.0
WORKDIR /src

# Project files are copied first, so package restore stays cached until they change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/TaskTracker.Contracts/TaskTracker.Contracts.csproj src/TaskTracker.Contracts/
COPY src/TaskTracker.Web/TaskTracker.Web.csproj src/TaskTracker.Web/
RUN dotnet restore src/TaskTracker.Web/TaskTracker.Web.csproj -p:TargetFrameworks=${DOTNET_TFM}

COPY src/TaskTracker.Contracts/ src/TaskTracker.Contracts/
COPY src/TaskTracker.Web/ src/TaskTracker.Web/
RUN dotnet publish src/TaskTracker.Web/TaskTracker.Web.csproj -c Release -f ${DOTNET_TFM} \
    -p:TargetFrameworks=${DOTNET_TFM} --no-restore -o /app/publish

# nginx generates this file from API_BASE_URL, so one image works with any API address.
RUN rm -f /app/publish/wwwroot/appsettings.json /app/publish/wwwroot/appsettings.json.br /app/publish/wwwroot/appsettings.json.gz

FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY docker/web/default.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 8080
