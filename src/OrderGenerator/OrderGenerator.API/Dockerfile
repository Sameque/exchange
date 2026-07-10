FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files to restore dependencies
COPY src/OrderGenerator/OrderGenerator.API/*.csproj src/OrderGenerator/OrderGenerator.API/
COPY src/OrderGenerator/OrderGenerator.Application/*.csproj src/OrderGenerator/OrderGenerator.Application/
COPY src/OrderGenerator/OrderGenerator.Infrastructure/*.csproj src/OrderGenerator/OrderGenerator.Infrastructure/
COPY src/OrderGenerator/OrderGenerator.Domain/*.csproj src/OrderGenerator/OrderGenerator.Domain/
COPY src/Shared/OpenTelemetry.Shared/*.csproj src/Shared/OpenTelemetry.Shared/

RUN dotnet restore src/OrderGenerator/OrderGenerator.API/OrderGenerator.API.csproj

# Copy the rest of the source code
COPY src/OrderGenerator src/OrderGenerator
COPY src/Shared/OpenTelemetry.Shared src/Shared/OpenTelemetry.Shared

WORKDIR /src/src/OrderGenerator/OrderGenerator.API
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderGenerator.API.dll"]
