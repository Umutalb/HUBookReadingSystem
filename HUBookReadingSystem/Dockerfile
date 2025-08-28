# Base runtime görüntüsü
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src


COPY ["HUBookReadingSystem/HUBookReadingSystem.csproj", "HUBookReadingSystem/"]
RUN dotnet restore "HUBookReadingSystem/HUBookReadingSystem.csproj"

COPY . .
WORKDIR "/src/HUBookReadingSystem"


RUN dotnet build "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HUBookReadingSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM base AS final
WORKDIR /app


RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "HUBookReadingSystem.dll"]