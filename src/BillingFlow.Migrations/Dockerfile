# File: src/BillingFlow.Migrations/Dockerfile

# --- STAGE 1: Base Runtime (Console App) ---
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

# --- STAGE 2: Build Environment ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Central Package Management and build property files FIRST
COPY ["Directory.Packages.props", "Directory.Build.props", "global.json*", "./"]

# Copy the migrations project
COPY ["src/BillingFlow.Migrations/BillingFlow.Migrations.csproj", "src/BillingFlow.Migrations/"]

# Restore dependencies
RUN dotnet restore "./src/BillingFlow.Migrations/BillingFlow.Migrations.csproj"

# Copy the rest of the source code (in case it references other projects or scripts)
COPY src/ ./src/

# Build the application
WORKDIR "/src/src/BillingFlow.Migrations"
RUN dotnet build "./BillingFlow.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- STAGE 3: Publish ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BillingFlow.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- STAGE 4: Final Image ---
FROM base AS final
WORKDIR /app
# Copy the compiled console app from the publish stage
COPY --from=publish /app/publish .

# Define the entry point (this will run the migrations and then the container will exit cleanly)
ENTRYPOINT ["dotnet", "BillingFlow.Migrations.dll"]