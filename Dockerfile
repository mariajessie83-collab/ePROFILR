# Base stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Server/Server.csproj", "Server/"]
COPY ["Gsystem/Gsystem.csproj", "Gsystem/"]
COPY ["SharedProject/SharedProject.projitems", "SharedProject/"]
COPY ["SharedProject/SharedProject.projitems.user", "SharedProject/"]

# Restore Server (which should also restore dependencies)
RUN dotnet restore "Server/Server.csproj"

# Copy the rest of the source code
COPY . .

# Build the Server project
WORKDIR "/src/Server"
RUN dotnet build "Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
