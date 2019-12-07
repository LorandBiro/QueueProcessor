FROM mcr.microsoft.com/dotnet/core/runtime:3.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["QueueProcessor.Reference/QueueProcessor.Reference.csproj", "QueueProcessor.Reference/"]
COPY ["QueueProcessor/QueueProcessor.csproj", "QueueProcessor/"]
RUN dotnet restore "QueueProcessor.Reference/QueueProcessor.Reference.csproj"
COPY . .
WORKDIR "/src/QueueProcessor.Reference"
RUN dotnet build "QueueProcessor.Reference.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QueueProcessor.Reference.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QueueProcessor.Reference.dll"]
