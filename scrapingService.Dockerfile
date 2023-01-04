#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TrainScrapingWorkerService/TrainScrapingWorkerService.csproj", "TrainScrapingWorkerService/"]
COPY ["TrainScrapingCommon/TrainScrapingCommon.csproj", "TrainScrapingCommon/"]
RUN dotnet restore "TrainScrapingWorkerService/TrainScrapingWorkerService.csproj"
COPY . .
WORKDIR "/src/TrainScrapingWorkerService"
RUN dotnet build "TrainScrapingWorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TrainScrapingWorkerService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrainScrapingWorkerService.dll"]