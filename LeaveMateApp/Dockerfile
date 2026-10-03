# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LeaveMate.sln .
COPY LeaveMate/LeaveMate.csproj LeaveMate/
COPY LeaveMate.Tests/LeaveMate.Tests.csproj LeaveMate.Tests/
RUN dotnet restore LeaveMate.sln

COPY . .
RUN dotnet publish LeaveMate/LeaveMate.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LeaveMate.dll"]
