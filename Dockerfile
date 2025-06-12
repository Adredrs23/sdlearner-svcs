# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project files and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy build output
COPY --from=build /app/out .

# Expose port (update if your API runs on a different port)
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "sdlearner-svcs.dll"]
