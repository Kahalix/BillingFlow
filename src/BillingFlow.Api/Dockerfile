# File: src/BillingFlow.Api/Dockerfile

# --- STAGE 1: Base Runtime (Lightweight image for running the app) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
# We expose port 8080 as configured in docker-compose.yml
EXPOSE 8080 

# --- STAGE 2: Build Environment (Heavy SDK image) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Central Package Management and build property files FIRST
COPY ["Directory.Packages.props", "Directory.Build.props", "global.json*", "./"]

# Copy project files to leverage Docker layer caching for NuGet packages
COPY ["src/BillingFlow.Api/BillingFlow.Api.csproj", "src/BillingFlow.Api/"]
COPY ["src/BillingFlow.Application/BillingFlow.Application.csproj", "src/BillingFlow.Application/"]
COPY ["src/BillingFlow.Domain/BillingFlow.Domain.csproj", "src/BillingFlow.Domain/"]
COPY ["src/BillingFlow.Infrastructure/BillingFlow.Infrastructure.csproj", "src/BillingFlow.Infrastructure/"]

# Restore dependencies (Now it sees the versions from Directory.Packages.props!)
RUN dotnet restore "./src/BillingFlow.Api/BillingFlow.Api.csproj"

# Copy the rest of the source code
COPY src/ ./src/

# Build the application
WORKDIR "/src/src/BillingFlow.Api"
RUN dotnet build "./BillingFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- STAGE 3: Publish (Optimized output) ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# /p:UseAppHost=false disables the generation of a native executable, saving space
RUN dotnet publish "./BillingFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- STAGE 4: Final Image ---
FROM base AS final
WORKDIR /app
# Copy the compiled application from the publish stage
COPY --from=publish /app/publish .

# Define the entry point for the container
ENTRYPOINT ["dotnet", "BillingFlow.Api.dll"]