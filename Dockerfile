FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinTrack.slnx ./
COPY src/FinTrack.Api/FinTrack.Api.csproj src/FinTrack.Api/
COPY src/FinTrack.Application/FinTrack.Application.csproj src/FinTrack.Application/
COPY src/FinTrack.Domain/FinTrack.Domain.csproj src/FinTrack.Domain/
COPY src/FinTrack.Infrastructure/FinTrack.Infrastructure.csproj src/FinTrack.Infrastructure/
COPY tests/FinTrack.Tests/FinTrack.Tests.csproj tests/FinTrack.Tests/

RUN dotnet restore src/FinTrack.Api/FinTrack.Api.csproj

COPY . .
RUN dotnet publish src/FinTrack.Api/FinTrack.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "FinTrack.Api.dll"]
