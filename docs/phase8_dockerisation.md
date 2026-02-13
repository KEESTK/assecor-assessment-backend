# Phase 8 — Dockerisation

## Goal

Containerise the application stack to ensure reproducible builds and environment-independent execution.

This demonstrates:

* Environment portability
* Infrastructure readiness
* Clean separation between build and runtime
* Docker-based workflow suitable for CI/CD

---

## 8.1 Backend Dockerfile (Multi-Stage)

**Purpose:** Build and run the API inside a container using a minimal runtime image.

### Design

Multi-stage build:

1. **Build stage**

   * Uses .NET SDK image
   * Restores dependencies
   * Builds solution
   * Publishes release artifacts

2. **Runtime stage**

   * Uses ASP.NET runtime image
   * Copies published output
   * Exposes HTTP port
   * Runs application

This ensures:

* Small runtime image
* Clean separation of concerns
* Deterministic builds


Create:
```bash
touch src/Assecor.Assessment.Api/Dockerfile
```
with more or less this dockerfile

```dockerfile
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
```

---

## 8.2 Ensure CSV is available inside container

In the Assecor.Assessment.Api.csproj, the following should be present according to prior steps:
```xml
  <ItemGroup>
    <None Include="..\..\sample-input.csv" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```
which means the CSV will be in the publish output and thus inside the container.

```xml
The previously configured runtime path also works then flwalessly with docker.
Path.Combine(AppContext.BaseDirectory, "sample-input.csv")
```
## 8.3 Production docker-compose.yml (db + api)
**Purpose:** Orchestrate full stack.

### Services

#### `db`

* Postgres 16
* Named volume
* Healthcheck
* Exposes 5432 internally

#### `api`

* Built from Dockerfile
* Depends on `db`
* Injects connection string via environment variables
* Exposes HTTP port (e.g. 8080)

### Environment Variables

Connection string passed via:

```
ConnectionStrings__Default
```

This keeps container configuration externalised and environment-specific.


Create/overwrite repo root file:

```bash
touch docker-compose.yml
```
replace with

```yaml
services:
  db:
    image: postgres:16
    container_name: assecor-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: assecor
    ports:
      - "5432:5432"
    volumes:
      - assecor_pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d assecor"]
      interval: 5s
      timeout: 3s
      retries: 10

  api:
    build:
      context: .
      dockerfile: src/Assecor.Assessment.Api/Dockerfile
    container_name: assecor-api
    depends_on:
      db:
        condition: service_healthy
    environment:
      ConnectionStrings__Default: "Host=db;Port=5432;Database=assecor;Username=postgres;Password=postgres"
      CSV_PATH: "sample-input.csv"

      # Development mode is intentionally enabled to expose Swagger UI
      # for assessment review and easier API verification.
      # In a real production deployment this would be set to "Production".
      ASPNETCORE_ENVIRONMENT: "Development"

    ports:
      - "8080:8080"

volumes:
  assecor_pgdata:
```

Notes:

DB hostname is db (compose service name).
We expose API on http://localhost:8080.
---


## 8.4 Ensuring migrations are applied in Docker

is achieved by extending Program.cs as follows;
the following code block is added right after 
```csharp 
var app = builder.Build(); 
```
```csharp 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```


## 8.5 End-to-end verification

From repo root:

```bash
docker compose up --build
```

Expected sucessfull stack output:

```bash
...
$ docker compose up --build
[+] Building 7.0s (20/20) FINISHED        
...
...
[+] Running 3/3
 ✔ assecor-assessment-backend-api  Built                      0.0s 
 ✔ Container assecor-postgres      Created                    0.1s 
 ✔ Container assecor-api           Created                    0.1s 
Attaching to assecor-api, assecor-postgres
assecor-postgres  | 
assecor-postgres  | PostgreSQL Database directory appears to contain a database; Skipping initialization
assecor-postgres  | 
...
...
...
...
assecor-api       |       Overriding HTTP_PORTS '8080' and HTTPS_PORTS ''. Binding to values defined by URLS instead 'http://+:8080'.
assecor-api       | info: Microsoft.Hosting.Lifetime[14]
assecor-api       |       Now listening on: http://[::]:8080
assecor-api       | info: Microsoft.Hosting.Lifetime[0]
assecor-api       |       Application started. Press Ctrl+C to shut down.
assecor-api       | info: Microsoft.Hosting.Lifetime[0]
assecor-api       |       Hosting environment: Development
assecor-api       | info: Microsoft.Hosting.Lifetime[0]
assecor-api       |       Content root path: /app
```
Then open:

http://localhost:8080/swagger


http://localhost:8080/persons


Expected:

* Postgres starts
* API waits for DB
* EF migrations applied
* CSV seeding runs
* Swagger available at:

```
http://localhost:8080/swagger
```

Expected Output:
```xml
Assecor.Assessment.Api

 1.0 

OAS 3.0

http://localhost:8080/swagger/v1/swagger.json
Persons
GET
/persons
POST
/persons
GET
/persons/{id}
GET
/persons/color/{color}
```
Also:
http://localhost:8080/persons
Expected Output:
```xml
JSON | Raw Data | Headers 
...
...
```
---
