# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LeaveMateApp/LeaveMate.sln LeaveMateApp/
COPY LeaveMateApp/LeaveMate/LeaveMate.csproj LeaveMateApp/LeaveMate/
COPY LeaveMateApp/LeaveMate.Tests/LeaveMate.Tests.csproj LeaveMateApp/LeaveMate.Tests/
RUN dotnet restore LeaveMateApp/LeaveMate.sln

COPY LeaveMateApp/ LeaveMateApp/
RUN dotnet publish LeaveMateApp/LeaveMate/LeaveMate.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LeaveMate.dll"]
