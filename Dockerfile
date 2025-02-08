# Base runtime image (ONLY .NET Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage (WITH .NET SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["DockerSSLWebAPI/DockerSSLWebAPI.csproj", "DockerSSLWebAPI/"]
RUN dotnet restore "./DockerSSLWebAPI/DockerSSLWebAPI.csproj"

# Copy the full source code
COPY . .

# Set working directory and build the app
WORKDIR "/src/DockerSSLWebAPI"
RUN dotnet build "./DockerSSLWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "./DockerSSLWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image (DO NOT INSTALL SDK HERE)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

# Run the Web API (No need for `dotnet-ef` here)
ENTRYPOINT ["dotnet", "DockerSSLWebAPI.dll"]
