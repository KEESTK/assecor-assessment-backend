# Phase 5 — CSV → DB Seeding on Startup

## Goal

Seed the PostgreSQL database from `sample-input.csv` automatically on application startup in an **idempotent** way.

---

## 5.1 Startup Seeder (Hosted Service)

**Purpose:** Import CSV data into the database when the API starts.

**Design intent:**

* CSV reading stays behind `IPersonCsvSource`.
* Mapping stays in Infrastructure (reuse Phase 3 importer).
* Seeding is idempotent via `CsvLineNumber` (no duplicate inserts).
* Works the same locally and in Docker.

---

### Implementation steps (essential)

1. Create folder `src/Assecor.Assessment.Infrastructure/Seeding/` via
   `mkdir -p src/Assecor.Assessment.Infrastructure/Seeding`.

2. Create file `src/Assecor.Assessment.Infrastructure/Seeding/CsvImportHostedService.cs` via
   `touch src/Assecor.Assessment.Infrastructure/Seeding/CsvImportHostedService.cs`.

3. Implement `CsvImportHostedService : IHostedService` that on startup:

   * reads CSV via `IPersonCsvSource`
   * maps to Domain `Person` (reuse `CsvPersonImporter`)
   * upserts into DB by `CsvLineNumber` (idempotent)
---

### 4. Register CSV seeding mechanism in the API project

**Design decision:**
The original `sample-input.csv` remains in the repository root (as required by the assessment).
During build, the file is automatically copied into the API output directory.
The application always reads the local runtime copy.

This avoids:

* fragile relative paths (`../../`)
* environment-specific logic
* runtime file manipulation

It ensures:

* identical behavior locally and in Docker
* no modification of the original CSV
* clean separation between repository artifacts and runtime resources

---

#### Step 4.1 — Copy CSV during build

Modify `src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj`:

```xml
<ItemGroup>
  <None Include="..\..\sample-input.csv" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

This guarantees that `sample-input.csv` is available inside:

```
bin/Debug/net9.0/
```

and later inside the Docker container.

---

---

### Step 4.2 — Configure CSV Path in `Program.cs`

The CSV file is copied into the API output directory during build.
Default read path uses AppContext.BaseDirectory

An optional environment variable (`CSV_PATH`) allows overriding the file location without changing application code.

```csharp
var csvPath =
    builder.Configuration["CSV_PATH"]
    ?? Path.Combine(AppContext.BaseDirectory, "sample-input.csv");

builder.Services.AddScoped<IPersonCsvSource>(_ => new CsvPersonSource(csvPath));
builder.Services.AddScoped<CsvPersonImporter>();

builder.Services.AddHostedService<CsvImportHostedService>();
```

---

### CSV Path Resolution Strategy

The CSV path is resolved as follows:

1. If the environment variable `CSV_PATH` is defined → use that file.
2. Otherwise → use `sample-input.csv` copied to the API output directory.

This ensures:

* No configuration is required for normal operation.
* The original `sample-input.csv` in the repository root remains unchanged.
* A different CSV file can be used without code modifications.
* Identical behavior locally and inside Docker containers.

---

### Default Behavior

When running locally or via Docker without setting `CSV_PATH`, the system automatically reads the copied `sample-input.csv`.

Setting `CSV_PATH` is optional and only required when using an alternative data source.

---

This makes the behavior:

* Deterministic by default
* Extensible when needed
* Fully aligned with the abstraction requirement

---


5. Build to verify compilation:

Fix: Update Infrastructure .csproj

Open: `src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj`

Add this ItemGroup under <Project ...>:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```
Then:

```bash
dotnet build -v minimal
```

---

## 5.2 Idempotency Rule

**Purpose:** Avoid duplicates on every restart.

**Rule:** A record is uniquely identified by `CsvLineNumber` (public API id).

---

### Where the rule is implemented

#### 1️⃣ Database schema (hard guarantee)

The database schema is defined in:

```text
Infrastructure/Persistence/AppDbContext.cs
```

Inside `OnModelCreating(...)`:

```csharp
entity.HasKey(e => e.Id);

entity.HasIndex(e => e.CsvLineNumber)
      .IsUnique();
```

This enforces a unique constraint on `CsvLineNumber`.

The schema is materialized in PostgreSQL via EF Core migrations, as outlined in **Phase 4.3**:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

#### 2️⃣ Application-level upsert logic (controlled behavior)

Implemented in:

```text
Infrastructure/Seeding/CsvImportHostedService.cs
```

Inside `StartAsync(...)`:

```csharp
var existing = await db.Persons
    .FirstOrDefaultAsync(x => x.CsvLineNumber == p.Id.Value, cancellationToken);

if (existing is null)
{
    db.Persons.Add(...);
}
else
{
    // update existing row
}
```

This ensures:

* If `CsvLineNumber` exists → update the row.
* If it does not exist → insert.
* No duplicate inserts occur.

---

### Resulting Guarantees

Idempotency is enforced on two levels:

* Database constraint (unique index)
* Application-level upsert logic

This guarantees:

* Stable row count across restarts
* No duplicate `CsvLineNumber`
* Deterministic startup behavior

---

## Verification

* Start API once → data inserted.
* Start API again → no additional rows created.
* Row count remains constant.
* No unique constraint violations occur.

---
