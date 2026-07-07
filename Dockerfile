FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TheGarden.slnx", "."]
COPY ["src/Garden.Api/Garden.Api.csproj", "src/Garden.Api/"]
COPY ["src/Garden.Engine/Garden.Engine.csproj", "src/Garden.Engine/"]
COPY ["src/Garden.Core/Garden.Core.csproj", "src/Garden.Core/"]
COPY ["src/Garden.World/Garden.World.csproj", "src/Garden.World/"]
COPY ["src/Garden.Infrastructure/Garden.Infrastructure.csproj", "src/Garden.Infrastructure/"]
COPY ["src/Garden.Contracts/Garden.Contracts.csproj", "src/Garden.Contracts/"]
COPY ["src/Garden.Shared/Garden.Shared.csproj", "src/Garden.Shared/"]
RUN dotnet restore "src/Garden.Api/Garden.Api.csproj"
COPY . .
WORKDIR "/src/src/Garden.Api"
RUN dotnet publish "Garden.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Garden.Api.dll"]