FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/ErrorOr/*.csproj src/ErrorOr/
COPY src/VsaResults.csproj src/
COPY src/VsaResults.Messaging/*.csproj src/VsaResults.Messaging/
COPY samples/VsaResults.Sample.WebApi/*.csproj samples/VsaResults.Sample.WebApi/
COPY tests/*.csproj tests/

# Restore dependencies
RUN dotnet restore samples/VsaResults.Sample.WebApi/VsaResults.Sample.WebApi.csproj

# Copy everything else
COPY . .

# Build
WORKDIR /src/samples/VsaResults.Sample.WebApi
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "VsaResults.Sample.WebApi.dll"]
