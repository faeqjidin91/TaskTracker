# Build from the repository root: docker build -f src/TaskTracker.Api/Dockerfile .
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG DOTNET_TFM=net10.0
WORKDIR /src

# Project files are copied first, so package restore stays cached until they change.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/TaskTracker.Domain/TaskTracker.Domain.csproj src/TaskTracker.Domain/
COPY src/TaskTracker.Contracts/TaskTracker.Contracts.csproj src/TaskTracker.Contracts/
COPY src/TaskTracker.Application/TaskTracker.Application.csproj src/TaskTracker.Application/
COPY src/TaskTracker.Infrastructure/TaskTracker.Infrastructure.csproj src/TaskTracker.Infrastructure/
COPY src/TaskTracker.Api/TaskTracker.Api.csproj src/TaskTracker.Api/
RUN dotnet restore src/TaskTracker.Api/TaskTracker.Api.csproj -p:TargetFrameworks=${DOTNET_TFM}

COPY src/TaskTracker.Domain/ src/TaskTracker.Domain/
COPY src/TaskTracker.Contracts/ src/TaskTracker.Contracts/
COPY src/TaskTracker.Application/ src/TaskTracker.Application/
COPY src/TaskTracker.Infrastructure/ src/TaskTracker.Infrastructure/
COPY src/TaskTracker.Api/ src/TaskTracker.Api/
RUN dotnet publish src/TaskTracker.Api/TaskTracker.Api.csproj -c Release -f ${DOTNET_TFM} \
    -p:TargetFrameworks=${DOTNET_TFM} -p:UseAppHost=false --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TaskTracker.Api.dll"]
