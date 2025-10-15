# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Render uses PORT environment variable
# Set a startup script to handle PORT variable
EXPOSE 10000

# Use shell form to allow environment variable expansion
CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet ProjectOrderNumberSystem.dll
