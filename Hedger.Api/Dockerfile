# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy solution + project files
COPY ../MetaExchange.sln ./
COPY ../Hedger.Core/Hedger.Core.csproj ./Hedger.Core/
COPY ./Hedger.Api.csproj ./Hedger.Api/

# restore
RUN dotnet restore ./Hedger.Api/Hedger.Api.csproj

# copy the rest of the source
COPY ../Hedger.Core ./Hedger.Core/
COPY . ./Hedger.Api/

# build & publish
WORKDIR /src/Hedger.Api
RUN dotnet publish -c Release -o /app/publish

# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# copy published output
COPY --from=build /app/publish ./

# expose port (change if your API uses another)
EXPOSE 8080

# run API
ENTRYPOINT ["dotnet", "Hedger.Api.dll"]
