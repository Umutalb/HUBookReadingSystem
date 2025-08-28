# Base runtime görüntüsü
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build görüntüsü
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY ["HUBookReadingSystem/HUBookReadingSystem.csproj", "HUBookReadingSystem/"]
RUN dotnet restore "HUBookReadingSystem/HUBookReadingSystem.csproj"

# Kaynak kodunu kopyala
COPY . .
WORKDIR "/src/HUBookReadingSystem"

# Projeyi build et
RUN dotnet build "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish aşaması
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final görüntü
FROM base AS final
WORKDIR /app

# Non-root user oluştur ve kullan
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Published dosyaları kopyala
COPY --from=publish /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "HUBookReadingSystem.dll"]