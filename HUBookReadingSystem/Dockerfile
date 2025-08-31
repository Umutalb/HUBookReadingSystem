# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore
COPY ["HUBookReadingSystem/HUBookReadingSystem.csproj", "HUBookReadingSystem/"]
RUN dotnet restore "HUBookReadingSystem/HUBookReadingSystem.csproj"

# Copy source code
COPY . .
WORKDIR "/src/HUBookReadingSystem"

# Build the project
RUN dotnet build "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app

# Create and use non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published files
COPY --from=publish /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "HUBookReadingSystem.dll"]