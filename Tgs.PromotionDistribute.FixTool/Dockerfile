#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.PromotionDistribute.FixTool/Tgs.PromotionDistribute.FixTool.csproj", "Tgs.PromotionDistribute.FixTool/"]
COPY ["FixTool.Core/FixTool.Core.csproj", "FixTool.Core/"]
COPY ["FixTool.Repository/FixTool.Repository.csproj", "FixTool.Repository/"]
RUN dotnet restore "Tgs.PromotionDistribute.FixTool/Tgs.PromotionDistribute.FixTool.csproj"
COPY . .
WORKDIR "/src/Tgs.PromotionDistribute.FixTool"
RUN dotnet build "Tgs.PromotionDistribute.FixTool.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.PromotionDistribute.FixTool.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.PromotionDistribute.FixTool.dll"]