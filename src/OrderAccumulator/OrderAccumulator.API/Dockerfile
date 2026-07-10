FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files to restore dependencies
COPY src/OrderAccumulator/OrderAccumulator.API/*.csproj src/OrderAccumulator/OrderAccumulator.API/
COPY src/OrderAccumulator/OrderAccumulator.Application/*.csproj src/OrderAccumulator/OrderAccumulator.Application/
COPY src/OrderAccumulator/OrderAccumulator.Infrastructure/*.csproj src/OrderAccumulator/OrderAccumulator.Infrastructure/
COPY src/OrderAccumulator/OrderAccumulator.Domain/*.csproj src/OrderAccumulator/OrderAccumulator.Domain/
COPY src/Shared/OpenTelemetry.Shared/*.csproj src/Shared/OpenTelemetry.Shared/

RUN dotnet restore src/OrderAccumulator/OrderAccumulator.API/OrderAccumulator.API.csproj

# Copy the rest of the source code
COPY src/OrderAccumulator src/OrderAccumulator
COPY src/Shared/OpenTelemetry.Shared src/Shared/OpenTelemetry.Shared

WORKDIR /src/src/OrderAccumulator/OrderAccumulator.API
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderAccumulator.API.dll"]
