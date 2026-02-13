# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj src/Assecor.Assessment.Domain/
COPY src/Assecor.Assessment.Application/Assecor.Assessment.Application.csproj src/Assecor.Assessment.Application/
COPY src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj src/Assecor.Assessment.Infrastructure/
COPY src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj src/Assecor.Assessment.Api/

RUN dotnet restore src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj

# Copy the rest
COPY . .

# Publish
RUN dotnet publish src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Bind to container port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Assecor.Assessment.Api.dll"]