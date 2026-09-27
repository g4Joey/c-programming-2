FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-ltsc2022 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-ltsc2022 AS build
ARG configuration=Release
WORKDIR /src
COPY ["LeaveMateApp/LeaveMate.Tests/LeaveMate.Tests.csproj", "LeaveMateApp/LeaveMate.Tests/"]
RUN dotnet restore "LeaveMateApp\LeaveMate.Tests\LeaveMate.Tests.csproj"
COPY . .
WORKDIR "/src/LeaveMateApp/LeaveMate.Tests"
RUN dotnet build "LeaveMate.Tests.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "LeaveMate.Tests.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaveMate.Tests.dll"]
