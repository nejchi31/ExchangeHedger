# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MetaExchange.sln ./
COPY Hedger.Core/Hedger.Core.csproj Hedger.Core/
COPY Hedger.Console/Hedger.Console.csproj Hedger.Console/

RUN dotnet restore Hedger.Console/Hedger.Console.csproj

# Copy the rest of the source
COPY Hedger.Core Hedger.Core/
COPY Hedger.Console Hedger.Console/

# Publish console app
WORKDIR /src/Hedger.Console
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish ./

# Run console app
ENTRYPOINT ["dotnet", "Hedger.Console.dll"]
