FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/UploadsApi.Domain/UploadsApi.Domain.csproj", "src/UploadsApi.Domain/"]
COPY ["src/UploadsApi.Application/UploadsApi.Application.csproj", "src/UploadsApi.Application/"]
COPY ["src/UploadsApi.Infrastructure/UploadsApi.Infrastructure.csproj", "src/UploadsApi.Infrastructure/"]
COPY ["src/UploadsApi.Api/UploadsApi.Api.csproj", "src/UploadsApi.Api/"]
RUN dotnet restore "src/UploadsApi.Api/UploadsApi.Api.csproj"

COPY . .
WORKDIR "/src/src/UploadsApi.Api"
RUN dotnet build "UploadsApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "UploadsApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

ENV \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

RUN apk add --no-cache \
    icu-data-full \
    icu-libs \
    curl

COPY --from=publish /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "UploadsApi.Api.dll"]