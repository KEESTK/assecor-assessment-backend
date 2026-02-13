
# Phase 7 — Tests (2.5–5h)

## Goal

Fulfill acceptance criterion:

> “Unit tests for the REST interface are available.”

All tests must pass via:

```bash
dotnet test
```

First, ensure you have the necessary testing frameworks I use:
```bash
dotnet add tests/Assecor.Assessment.Tests package xunit
dotnet add tests/Assecor.Assessment.Tests package xunit.runner.visualstudio
dotnet add tests/Assecor.Assessment.Tests package Microsoft.NET.Test.Sdk
dotnet add tests/Assecor.Assessment.Tests package Microsoft.AspNetCore.Mvc.Testing
```
---

## 7.1 Domain Tests (Unit)

**Purpose:** Validate core invariants and mapping logic without infrastructure.

### Coverage

* `FavouriteColour`

  * Normalization (trim, case-insensitive)
  * ASCII variants accepted (`gruen`, `tuerkis`, `weiss`)
  * Reject unknown colours (throws)

* `PersonId`

  * Enforces invariant `value > 0` (throws for `<= 0`)

* Colour code mapping (1–7)

  * Correct mapping to canonical German colour names
  * Unsupported code throws

1) Create folders + files

From repo root:
```bash
mkdir -p tests/Assecor.Assessment.Tests/Domain/Persons
mkdir -p tests/Assecor.Assessment.Tests/Domain/Colours

touch tests/Assecor.Assessment.Tests/Domain/Persons/PersonIdTests.cs
touch tests/Assecor.Assessment.Tests/Domain/Colours/FavouriteColourTests.cs
touch tests/Assecor.Assessment.Tests/Domain/Colours/ColourCodeMapperTests.cs
```
2) Implement tests
What these tests verify

PersonIdTests
```text
Verifies the PersonId value object enforces its invariant (> 0) and preserves the provided value.
```
FavouriteColourTests
```text
Verifies FavouriteColour.From(...):
normalizes input (trim + case-insensitive)
accepts ASCII variants (gruen, tuerkis, weiss) and maps to canonical German names
rejects unsupported/invalid colours by throwing
```


ColourCodeMapperTests
```text
Verifies ColourCodeMapper.FromCode(int) correctly maps numeric codes 1–7 to canonical colours and throws for unsupported codes.
```

3) Run domain tests

```bash
dotnet test tests/Assecor.Assessment.Tests -v minimal
```

Sample Output:
```bash
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.5+1b188a7b0a (64-bit .NET 9.0.1)
[xUnit.net 00:00:00.04]   Discovering: Assecor.Assessment.Tests
[xUnit.net 00:00:00.07]   Discovered:  Assecor.Assessment.Tests
[xUnit.net 00:00:00.09]   Starting:    Assecor.Assessment.Tests
[xUnit.net 00:00:00.87]   Finished:    Assecor.Assessment.Tests
  Assecor.Assessment.Tests test succeeded (3,6s)

Test summary: total: 32, failed: 0, succeeded: 32, skipped: 0, duration: 3,6s
```

---

## 7.2 API Integration Tests (WebApplicationFactory + Test Postgres)

**Purpose:** Test the REST interface end-to-end (routing, validation, serialization, persistence).

### Test Database Strategy (Dedicated Compose DB)

* Create the test DB compose file
From repo root:
```bash
touch docker-compose.test.yml
```
with following config:

```yaml
services:
  postgres_test:
    image: postgres:16
    container_name: assecor-postgres-test
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: assecor_test
    ports:
      - "5433:5432"
    volumes:
      - assecor_pgdata_test:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d assecor_test"]
      interval: 5s
      timeout: 3s
      retries: 10

volumes:
  assecor_pgdata_test:
```

Cool — A it is. Here’s the **exact implementation** for 7.2 with TRUNCATE reset + dedicated test Postgres on `5433`.

---

## 7.2.1 Start test Postgres

(You already created `docker-compose.test.yml`.)

```bash
docker compose -f docker-compose.test.yml up -d
docker compose -f docker-compose.test.yml ps
```

---

## 7.2.2 Add integration test infrastructure

### 1) Disable test parallelization (important for TRUNCATE strategy)

Create:

```bash
touch tests/Assecor.Assessment.Tests/xunit.runner.json
```

Put this JSON:

```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

Then ensure your test `.csproj` copies it:

Open:

```bash
code tests/Assecor.Assessment.Tests/Assecor.Assessment.Tests.csproj
```

Add:

```xml
<ItemGroup>
  <None Update="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

### 2) Custom WebApplicationFactory (points API to test DB and disables CSV seeding)

Purpose:
Boot the real API application inside the test process, but override production infrastructure.

What it does compared to production:
Uses a dedicated test Postgres database (assecor_test, port 5433).
Removes the production CsvImportHostedService (no CSV auto-seeding).
Applies EF Core migrations programmatically for the test DB.
Keeps all other wiring identical (controllers, repository, DI).

This ensures:
The full HTTP pipeline is tested (routing, controllers, serialization).
Tests do not depend on the production database.
No CSV file is touched during integration tests.

Infrastructure is replaceable via DI (as required by acceptance criteria).
Create:

```bash
mkdir -p tests/Assecor.Assessment.Tests/Infrastructure
touch tests/Assecor.Assessment.Tests/Infrastructure/CustomWebApplicationFactory.cs
```

code is as implemented.

---

### 3) DB reset helper (TRUNCATE)

Purpose:
Guarantee deterministic test behavior between test cases.
What it does:

Executes:
TRUNCATE "Persons" RESTART IDENTITY;
before each test.

This ensures:
No leftover data from previous tests.
Predictable ID values.
Idempotent test execution.
Stable results across repeated dotnet test runs.
Parallel execution is disabled to prevent race conditions.

Create:

```bash
touch tests/Assecor.Assessment.Tests/Infrastructure/DbReset.cs
```

code is as implemented.

---

## 7.2.3 Create the integration tests (REST interface)
Purpose:
Verify that the REST API behaves exactly as specified.

These tests validate the full stack:
HTTP → Controller → Application Port → EF Repository → PostgreSQL

Each test:
Seeds controlled test data directly into the test DB.
Sends real HTTP requests using HttpClient.

Asserts:
Correct HTTP status codes (200, 201, 400, 404)
Correct JSON structure
Correct business behavior (filtering, ID assignment, validation)
What is verified against the specification:
GET /persons returns full list.
GET /persons/{id} returns 404 when missing.
GET /persons/color/{color} is case-insensitive and filters correctly.
POST /persons:
    assigns max + 1 ID,
    validates colour,
    returns 201 Created.
This fulfills the acceptance criterion:
    “Unit tests for the REST interface are available.”

and demonstrates:

Proper dependency injection
Replaceable infrastructure
Deterministic behavior
Correct HTTP contract implementation

Create:

```bash
mkdir -p tests/Assecor.Assessment.Tests/Api
touch tests/Assecor.Assessment.Tests/Api/PersonsApiTests.cs
```

code is as implemented.

---

## 7.2.4 Run tests

FIXES:
Fix : Microsoft.AspNetCore.Mvc.Testing missing in test project
```bash
dotnet add tests/Assecor.Assessment.Tests/Assecor.Assessment.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 9.0.4
dotnet restore
dotnet list tests/Assecor.Assessment.Tests package | grep Testing || true
```
Fix: add a public partial Program in API project
```bash
touch src/Assecor.Assessment.Api/ProgramVisibility.cs
```
```csharp
namespace Assecor.Assessment.Api;

public partial class Program { }
```
plus changes to WebApplicationFactory<Program>

then RUN:
```bash
dotnet test tests/Assecor.Assessment.Tests -v minimal
```

Expected (/My) Output:
```bash
[xUnit.net 00:00:46.09]   Finished:    Assecor.Assessment.Tests
  Assecor.Assessment.Tests test succeeded (46,8s)

Test summary: total: 38, failed: 0, succeeded: 38, skipped: 0, duration: 46,8s
Build succeeded with 1 warning(s) in 73,6s
```
## 7.3 Added a final POST API Test Case


### `POST /persons` (bonus)

* Valid request:

  * `201 Created`
  * Response body contains created person
  * `id` assigned as `max + 1`
* Invalid request:

  * invalid colour → `400 Bad Request`
  * missing required fields → `400 Bad Request` (THIS ONE)

---

## 7.4 Final Verification Step

1. Start test DB:

```bash
docker compose -f docker-compose.test.yml up -d
```

2. Run tests:

```bash
dotnet test
```

Expected:

* All tests green
* No dependency on development DB
* Deterministic results across repeated runs

sample output:
```bash
$ dotnet test
Restore complete (0,6s)
  Assecor.Assessment.Domain succeeded (0,1s) → src/Assecor.Assessment.Domain/bin/Debug/net9.0/Assecor.Assessment.Domain.dll
  Assecor.Assessment.Application succeeded (0,0s) → src/Assecor.Assessment.Application/bin/Debug/net9.0/Assecor.Assessment.Application.dll
  Assecor.Assessment.Infrastructure succeeded (0,1s) → src/Assecor.Assessment.Infrastructure/bin/Debug/net9.0/Assecor.Assessment.Infrastructure.dll
  Assecor.Assessment.Api succeeded (0,2s) → src/Assecor.Assessment.Api/bin/Debug/net9.0/Assecor.Assessment.Api.dll
  Assecor.Assessment.Tests succeeded ...
  ...
  ...
[xUnit.net 00:00:01.24]   Finished:    Assecor.Assessment.Tests
  Assecor.Assessment.Tests test succeeded (1,7s)

Test summary: total: 39, failed: 0, succeeded: 39, skipped: 0, duration: 1,7s
Build succeeded with 1 warning(s) in 3,2s
```
---
