FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Tester/Tester.csproj", "Tester/"]
COPY ["Tests/Tests.csproj", "Tests/"]
COPY ["KinesisSharp/KinesisSharp.csproj", "KinesisSharp/"]
RUN dotnet restore "Tester/Tester.csproj"
COPY . .
WORKDIR "/src/Tester"
RUN dotnet build "Tester.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Tester.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Tester.dll"]