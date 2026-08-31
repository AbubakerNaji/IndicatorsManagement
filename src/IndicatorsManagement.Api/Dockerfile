# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY IndicatorsManagement.slnx ./
COPY src/IndicatorsManagement.Domain/*.csproj src/IndicatorsManagement.Domain/
COPY src/IndicatorsManagement.Contracts/*.csproj src/IndicatorsManagement.Contracts/
COPY src/IndicatorsManagement.Application/*.csproj src/IndicatorsManagement.Application/
COPY src/IndicatorsManagement.Infrastructure/*.csproj src/IndicatorsManagement.Infrastructure/
COPY src/IndicatorsManagement.Api/*.csproj src/IndicatorsManagement.Api/

RUN dotnet restore IndicatorsManagement.slnx

# Copy everything and build
COPY src/ src/
WORKDIR /src/src/IndicatorsManagement.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user (Debian/Ubuntu-style — the .NET 10 base image uses Debian).
RUN groupadd --system appgroup && useradd --system --gid appgroup --create-home appuser

COPY --from=build /app/publish .

# Create directories for uploads and logs
RUN mkdir -p /app/uploads /app/logs && chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "IndicatorsManagement.Api.dll"]
