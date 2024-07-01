FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release

WORKDIR /src
COPY "Directory.Build.props" .
COPY "NuGet.config" "src/"
COPY "libs" "src/libs"
COPY "src/Chatbot/Chatbot.fsproj" "src/Chatbot/"
COPY "src/Chatbot.Commands/Chatbot.Commands.fsproj" "src/Chatbot.Commands/"
COPY "src/Chatbot.Core/Chatbot.Core.fsproj" "src/Chatbot.Core/"
COPY "src/Chatbot.Database/Chatbot.Database.fsproj" "src/Chatbot.Database/"
RUN dotnet restore "src/Chatbot/Chatbot.fsproj"

COPY . .
WORKDIR /src
RUN dotnet build "src/Chatbot/Chatbot.fsproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "src/Chatbot/Chatbot.fsproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chatbot.dll"]
