# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY HedgerExchange.sln ./
COPY Hedger.Core/Hedger.Core.csproj Hedger.Core/
COPY Hedger.Api/Hedger.Api.csproj Hedger.Api/

# Restore only for the API project
RUN dotnet restore Hedger.Api/Hedger.Api.csproj

# Copy the rest of the source
COPY Hedger.Core Hedger.Core/
COPY Hedger.Api Hedger.Api/

# Publish API
WORKDIR /src/Hedger.Api
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "Hedger.Api.dll"]
