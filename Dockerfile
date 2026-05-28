# Multi-stage build — küçük production image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj files for layer caching
COPY src/Folkie.Api/Folkie.Api.csproj src/Folkie.Api/
COPY src/Folkie.Application/Folkie.Application.csproj src/Folkie.Application/
COPY src/Folkie.Domain/Folkie.Domain.csproj src/Folkie.Domain/
COPY src/Folkie.Infrastructure/Folkie.Infrastructure.csproj src/Folkie.Infrastructure/
COPY Folkie.sln .

RUN dotnet restore Folkie.sln

# Copy source + publish
COPY . .
RUN dotnet publish src/Folkie.Api/Folkie.Api.csproj -c Release -o /app /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

# Non-root user
RUN addgroup -S folkie && adduser -S folkie -G folkie
USER folkie

# Data Protection keys persistent volume
VOLUME ["/app/keys"]

ENV ASPNETCORE_URLS=http://+:5069
EXPOSE 5069

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:5069/healthz || exit 1

ENTRYPOINT ["dotnet", "Folkie.Api.dll"]
