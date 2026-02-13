
```bash
touch docs/phase4_efcore_postgres.md
```
---
# Phase 4 — EF Core + PostgreSQL

## Goal

Introduce a relational datastore using **PostgreSQL + EF Core** and implement a repository that satisfies `IPersonRepository`.

This phase prepares the system for:

* Persistent storage
* CSV seeding
* `POST /persons`
* Future scalability

---

## 4.1 EF Core Setup

**Purpose:** Add database infrastructure and configure EF Core.

**Design intent:**

* Infrastructure layer contains all persistence concerns.
* Domain remains persistence-agnostic.
* Database stores `CsvLineNumber` as the public `PersonId`.

---

### Implementation steps (essential)

1. Add EF Core packages to Infrastructure project:

```bash
dotnet add src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj package Microsoft.EntityFrameworkCore --version 9.0.4
dotnet add src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet add src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4

dotnet restore src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj
dotnet list src/Assecor.Assessment.Infrastructure package
```

2. Create folder `src/Assecor.Assessment.Infrastructure/Persistence/` via
   `mkdir -p src/Assecor.Assessment.Infrastructure/Persistence`.

3. Create file `src/Assecor.Assessment.Infrastructure/Persistence/AppDbContext.cs` via
   `touch src/Assecor.Assessment.Infrastructure/Persistence/AppDbContext.cs`.

4. Implement `AppDbContext` inheriting from `DbContext`.

5. Build to verify compilation:

```bash
dotnet build src/Assecor.Assessment.Infrastructure -v minimal
```

---

## 4.2 EF Entity Model

**Purpose:** Define persistence representation of `Person`.

**Design decision:**

* Persistence model mirrors Domain model.
* Store `CsvLineNumber` as a unique column.
* Use internal DB primary key if desired, but API must expose `CsvLineNumber`.

---

### Implementation steps (essential)

1. Create file `src/Assecor.Assessment.Infrastructure/Persistence/Entities/PersonEntity.cs` via:

```bash
mkdir -p src/Assecor.Assessment.Infrastructure/Persistence/Entities
touch src/Assecor.Assessment.Infrastructure/Persistence/Entities/PersonEntity.cs
```

2. Define properties:

   * `int Id` (DB primary key)
   * `int CsvLineNumber` (unique)
   * `string FirstName`
   * `string LastName`
   * `string ZipCode`
   * `string City`
   * `string Colour`

3. Configure unique constraint on `CsvLineNumber` in `OnModelCreating`.

4. Build to verify compilation.

```bash
dotnet build src/Assecor.Assessment.Infrastructure -v minimal
```

---

## 4.3 Migrations

**Purpose:** Create and apply the database schema via EF Core migrations.

---

### Implementation steps (essential)

1. Install EF tooling (version aligned with EF Core 9) via:

```bash
dotnet tool install --global dotnet-ef --version 9.0.9
# or, if already installed:
dotnet tool update --global dotnet-ef --version 9.0.9
```

2. Verify installation via:

```bash
dotnet ef --version
```

3. Create initial migration via:
Fix: startup project package

```bash
dotnet add src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.4
dotnet restore src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj
```

Fix: Add IDesignTimeDbContextFactory<AppDbContext>
```bash
touch src/Assecor.Assessment.Infrastructure/Persistence/AppDbContextFactory.cs
```
and configure connectionString as follows: 
```csharp
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ASSESSMENT_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=assecor;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
```
then migrate:


```bash
dotnet ef migrations add InitialCreate \
  --project src/Assecor.Assessment.Infrastructure \
  --startup-project src/Assecor.Assessment.Api
```


4. Apply migration (requires configured DB connection) via:

```bash
dotnet ef database update \
  --project src/Assecor.Assessment.Infrastructure \
  --startup-project src/Assecor.Assessment.Api
```

---

## 4.4 Implement EfPersonRepository

**Purpose:** Provide EF-backed implementation of `IPersonRepository`.

**Design intent:**

* Repository translates between Domain `Person` and `PersonEntity`.
* Infrastructure handles mapping.
* Application remains unaware of EF.

---

### Implementation steps (essential)

1. Create file `src/Assecor.Assessment.Infrastructure/Persistence/EfPersonRepository.cs` via:

```bash
touch src/Assecor.Assessment.Infrastructure/Persistence/EfPersonRepository.cs
```

2. Implement `EfPersonRepository : IPersonRepository` with:

   * `GetAllAsync`
   * `GetByIdAsync`
   * `GetByColourAsync`
   * `AddAsync`
3. Map:

   * `PersonId.Value ↔ CsvLineNumber`
   * `FavouriteColour.Value ↔ Colour`
4. Build to verify compilation:

```bash
dotnet build src/Assecor.Assessment.Infrastructure -v minimal
```

---

Here is **4.5 rewritten in the same clean, structured style** as your previous phases.

---

## 4.5 Dependency Injection Wiring

**Purpose:** Register the persistence layer in the API (Composition Root) and configure database access.

**Design intent:**

* `Program.cs` acts as the composition root.
* Infrastructure is registered via DI.
* Connection string is provided via configuration.
* Application layer remains unaware of EF Core.

---

### Implementation steps (essential)

---

1. Configure PostgreSQL connection strings in the API project.

Use environment-based configuration to support both local development and Docker.

`src/Assecor.Assessment.Api/appsettings.Development.json` (local):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=assecor;Username=postgres;Password=postgres"
  }
}
```

`src/Assecor.Assessment.Api/appsettings.json` (Docker/default):

```json
{
  "ConnectionStrings": {
    "Default": "Host=postgres;Port=5432;Database=assecor;Username=postgres;Password=postgres"
  }
}
```

This allows:

* `localhost` when running locally.
* `postgres` when running inside Docker (container hostname).
* No code changes between environments.


---

Here’s the refactored section (concise, same style), **plus** the point of the middleware lines.

---

2. Configure DI and routing in `src/Assecor.Assessment.Api/Program.cs`.

* Add required `using` statements:

```csharp
using Assecor.Assessment.Application.Ports;
using Assecor.Assessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
```

* Register controllers (required for attribute-routed controllers):

```csharp
builder.Services.AddControllers();
```

* Register `AppDbContext` (Postgres via Npgsql) and fail fast if missing config:

```csharp
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing connection string: ConnectionStrings:Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

* Register repository:

```csharp
builder.Services.AddScoped<IPersonRepository, EfPersonRepository>();
```

* Enable request pipeline + endpoint routing:

```csharp
app.UseHttpsRedirection();
app.MapControllers();
```

**Notes:**

* `app.UseHttpsRedirection();` redirects HTTP → HTTPS (common production default; harmless locally).
* `app.MapControllers();`  maps my controller endpoints; without it, my REST controllers won’t be reachable.

---


3. Build full solution to verify wiring:

```bash
dotnet build -v minimal
```
---

## Deliverables of Phase 4

* EF Core configured
* PostgreSQL provider added
* Migration created
* `EfPersonRepository` implemented
* Repository wired via DI
* Infrastructure ready for CSV seeding

---
