FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cosmos.DatosReferencia.Comandos.API/Cosmos.DatosReferencia.Comandos.API.csproj", "Cosmos.DatosReferencia.Comandos.API/"]
RUN dotnet restore "Cosmos.DatosReferencia.Comandos.API/Cosmos.DatosReferencia.Comandos.API.csproj"
COPY . .
WORKDIR "/src/Cosmos.DatosReferencia.Comandos.API"
RUN dotnet build "Cosmos.DatosReferencia.Comandos.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Cosmos.DatosReferencia.Comandos.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cosmos.DatosReferencia.Comandos.API.dll"]
