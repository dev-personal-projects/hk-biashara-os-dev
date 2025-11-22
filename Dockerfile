# Use the official .NET 9.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/ApiWorker/*.csproj ./src/ApiWorker/
RUN dotnet restore src/ApiWorker/ApiWorker.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish src/ApiWorker/ApiWorker.csproj -c Release -o out

# Use the official .NET 9.0 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install Azure CLI (needed for Azure authentication)
RUN apt-get update && apt-get install -y \
    curl \
    apt-transport-https \
    lsb-release \
    gnupg \
    && curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
    && rm -rf /var/lib/apt/lists/*

# Copy the published app
COPY --from=build /app/out .

# Expose port (default to 8080, can be overridden by environment variables)
EXPOSE 8080

# Set environment variables
# Note: These can be overridden by Azure Container Apps environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "ApiWorker.dll"]