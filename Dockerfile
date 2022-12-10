#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# amd64
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
# arm64v8
#FROM mcr.microsoft.com/dotnet/nightly/runtime:7.0-jammy-arm64v8 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DiscordIrcBridge.csproj", "."]
RUN dotnet restore "./DiscordIrcBridge.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DiscordIrcBridge.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordIrcBridge.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
LABEL product="Discord IRC Bridge"
LABEL vendor="Scott Blomfield"
LABEL email="github@cyberknet.net"
LABEL github_url="https://github.com/cyberknet/discordircbridge"
WORKDIR /app
VOLUME ["/data"]
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DiscordIrcBridge.dll"]