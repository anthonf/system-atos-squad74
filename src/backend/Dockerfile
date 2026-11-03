# syntax=docker/dockerfile:1

# -----------------------------------------------------------------------------
# Build stage
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy only the project files first so NuGet restore is cached independently
# from source code changes.
COPY Directory.Build.props ./
COPY src/SupportAtos.Api/SupportAtos.Api.csproj ./src/SupportAtos.Api/
COPY src/SupportAtos.Application/SupportAtos.Application.csproj ./src/SupportAtos.Application/
COPY src/SupportAtos.Domain/SupportAtos.Domain.csproj ./src/SupportAtos.Domain/
COPY src/SupportAtos.Infrastructure/SupportAtos.Infrastructure.csproj ./src/SupportAtos.Infrastructure/

RUN dotnet restore src/SupportAtos.Api/SupportAtos.Api.csproj

# Copy the remaining source code and publish in Release mode.
COPY . .

RUN dotnet publish src/SupportAtos.Api/SupportAtos.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Runtime stage
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Listen on all interfaces; Render/other platforms can override with $PORT.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

# Run as the non-root user provided by the official ASP.NET image.
USER $APP_UID

ENTRYPOINT ["dotnet", "SupportAtos.Api.dll"]
