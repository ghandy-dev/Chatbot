FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

USER 1654[:1654]
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release

WORKDIR /src
COPY ["src/Chatbot/Chatbot.fsproj", "src/Chatbot/"]
RUN dotnet restore "src/Chatbot/Chatbot.fsproj"

COPY . .
WORKDIR "/src/src/Chatbot"
RUN dotnet build "Chatbot.fsproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Chatbot.fsproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chatbot.dll"]
