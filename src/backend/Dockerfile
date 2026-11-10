# syntax=docker/dockerfile:1
#
# Dockerfile da SupportAtos.Api (.NET 10).
# Contexto de build: src/backend/  (onde ficam este arquivo, Directory.Build.props e src/)
#
#   docker build -t supportatos-api .
#   docker run --rm -p 8080:8080 \
#     -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=supportatos;Username=...;Password=..." \
#     supportatos-api

# ---------------------------------------------------------------------------
# Stage 1: build/publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia só os .csproj (e o Directory.Build.props) para que o restore fique em
# cache enquanto as dependências NuGet não mudarem.
COPY Directory.Build.props ./
COPY src/SupportAtos.Domain/SupportAtos.Domain.csproj                 src/SupportAtos.Domain/
COPY src/SupportAtos.Application/SupportAtos.Application.csproj       src/SupportAtos.Application/
COPY src/SupportAtos.Infrastructure/SupportAtos.Infrastructure.csproj src/SupportAtos.Infrastructure/
COPY src/SupportAtos.Api/SupportAtos.Api.csproj                       src/SupportAtos.Api/

# Restaura só a API (as demais entram como referências de projeto).
RUN dotnet restore src/SupportAtos.Api/SupportAtos.Api.csproj

# Agora sim o código-fonte.
COPY src/ src/

RUN dotnet publish src/SupportAtos.Api/SupportAtos.Api.csproj \
      -c ${BUILD_CONFIGURATION} \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Stage 2: runtime (imagem enxuta, sem SDK)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# A imagem aspnet já escuta em 8080 (ASPNETCORE_HTTP_PORTS=8080) e traz o
# usuário não-root "app" (variável APP_UID).
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

# A imagem não tem curl/wget; usa o /dev/tcp do bash para chamar /health.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080 && printf "GET /health HTTP/1.0\r\nHost: localhost\r\n\r\n" >&3 && head -n1 <&3 | grep -q " 200 "'

ENTRYPOINT ["dotnet", "SupportAtos.Api.dll"]
