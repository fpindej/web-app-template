# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["MyProject.WebApi/MyProject.WebApi.csproj", "MyProject.WebApi/"]
RUN dotnet restore "MyProject.WebApi/MyProject.WebApi.csproj"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR "/src/MyProject.WebApi"
RUN dotnet build "MyProject.WebApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MyProject.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:DebugType=None /p:DebugSymbols=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MyProject.WebApi.dll"]
