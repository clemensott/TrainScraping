#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TrainScrapingApi/TrainScrapingApi.csproj", "TrainScrapingApi/"]
COPY ["TrainScrapingCommon/TrainScrapingCommon.csproj", "TrainScrapingCommon/"]
RUN ls TrainScrapingApi/
RUN dotnet restore "TrainScrapingApi/TrainScrapingApi.csproj"
COPY . .
WORKDIR "/src/TrainScrapingApi"
RUN dotnet build "TrainScrapingApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TrainScrapingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrainScrapingApi.dll"]