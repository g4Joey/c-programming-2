FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration=Release
WORKDIR /src

COPY ["LeaveMateApp/LeaveMate/LeaveMate.csproj", "LeaveMateApp/LeaveMate/"]
COPY ["LeaveMateApp/LeaveMate.Tests/LeaveMate.Tests.csproj", "LeaveMateApp/LeaveMate.Tests/"]
RUN dotnet restore "LeaveMateApp/LeaveMate.Tests/LeaveMate.Tests.csproj"

COPY . .
WORKDIR "/src/LeaveMateApp/LeaveMate.Tests"
RUN dotnet test "LeaveMate.Tests.csproj" -c $configuration --logger "console;verbosity=minimal"
