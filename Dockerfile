FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["InventorySystem.API/InventorySystem.API.csproj", "InventorySystem.API/"]
COPY ["InventorySystem.Application/InventorySystem.Application.csproj", "InventorySystem.Application/"]
COPY ["InventorySystem.Core/InventorySystem.Core.csproj", "InventorySystem.Core/"]
COPY ["InventorySystem.Infrastructure/InventorySystem.Infrastructure.csproj", "InventorySystem.Infrastructure/"]

RUN dotnet restore "InventorySystem.API/InventorySystem.API.csproj"

# Copy all source code and build
COPY . .
WORKDIR "/src/InventorySystem.API"
RUN dotnet build "InventorySystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InventorySystem.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway requires the app to listen on the PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT
ENTRYPOINT ["dotnet", "InventorySystem.API.dll"]
