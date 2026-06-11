# Use the .NET 9 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything and build the project
COPY . .

# Notice the added "LeafBy/" folder path here!
RUN dotnet restore "LeafBy/LeafBy.csproj"
RUN dotnet publish "LeafBy/LeafBy.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the .NET 9 Runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port Render expects
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "LeafBy.dll"]