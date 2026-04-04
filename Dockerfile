# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["BG.sln", "."]
COPY ["src/BG.Web/BG.Web.csproj", "src/BG.Web/"]
COPY ["src/BG.Application/BG.Application.csproj", "src/BG.Application/"]
COPY ["src/BG.Domain/BG.Domain.csproj", "src/BG.Domain/"]
COPY ["src/BG.Infrastructure/BG.Infrastructure.csproj", "src/BG.Infrastructure/"]
COPY ["src/BG.Integrations/BG.Integrations.csproj", "src/BG.Integrations/"]

# Restore dependencies (excluding tests to speed up build)
RUN dotnet restore "src/BG.Web/BG.Web.csproj"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "src/BG.Web/BG.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/BG.Web/BG.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "BG.Web.dll"]
