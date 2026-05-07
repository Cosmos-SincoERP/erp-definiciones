FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cosmos.DatosReferencia.Comandos.MCP.Server/Cosmos.DatosReferencia.Comandos.MCP.Server.csproj", "Cosmos.DatosReferencia.Comandos.MCP.Server/"]
RUN dotnet restore "Cosmos.DatosReferencia.Comandos.MCP.Server/Cosmos.DatosReferencia.Comandos.MCP.Server.csproj"
COPY . .
WORKDIR "/src/Cosmos.DatosReferencia.Comandos.MCP.Server"
RUN dotnet build "./Cosmos.DatosReferencia.Comandos.MCP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cosmos.DatosReferencia.Comandos.MCP.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cosmos.DatosReferencia.Comandos.MCP.Server.dll"]
