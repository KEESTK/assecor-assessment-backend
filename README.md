# TECHNICAL DOCUMENTATION

# 1. Overview

This project implements a RESTful backend for managing persons and their favourite colour according to the Assecor Assessment specification.

The solution is built using **.NET 9 (C#)** and is strongly structured around **Domain-Driven Design (DDD)** and **Clean Architecture principles**.

Key characteristics:

* Clear domain model with enforced invariants
* Infrastructure fully abstracted behind interfaces
* Deterministic startup behavior
* Full integration test coverage
* Complete Docker-based runtime setup

The system:

* Reads immutable input data from a CSV file
* Maps it into a rich domain model
* Persists it into PostgreSQL
* Exposes a RESTful API
* Is fully tested end-to-end
* Is fully containerised

---

# 2. Architectural Philosophy

## Heavy Use of Domain-Driven Design (DDD)

This solution is intentionally domain-centric.

Business rules are not implemented in controllers or infrastructure, but encapsulated inside:

* **Value Objects**
* **Domain entities**
* **Domain mapping logic**

The domain layer:

* Enforces invariants (`PersonId > 0`)
* Normalizes and validates colours
* Controls canonical representation
* Maps colour codes to domain concepts

The REST API and database are merely delivery and persistence mechanisms around the core domain.

This ensures:

* Business logic remains stable
* Infrastructure can evolve independently
* Tests can validate logic without external dependencies

---

# 3. Requirement Mapping (With Implementation Depth)

## 3.1 Manage Persons and Favourite Colour

### Implemented Solution

* Dedicated `PersonsController`
* DTO contracts for request/response separation
* Repository abstraction via `IPersonRepository`
* Domain entity `Person`

Endpoints:

* `GET /persons`
* `GET /persons/{id}`
* `GET /persons/color/{color}`
* `POST /persons` (bonus)

### Why This Design

The controller depends only on the repository interface.

It does not:

* Access EF Core
* Access DbContext
* Parse CSV
* Perform business validation

All validation and normalization are delegated to the domain model, ensuring separation of concerns and architectural clarity.

---

## 3.2 CSV as Immutable Source

### Implemented Solution

* `IPersonCsvSource` interface
* `CsvPersonSource` implementation
* `ColourCodeMapper` (maps 1–7 to domain value object)
* MSBuild copies CSV to runtime directory
* Idempotent seeding on startup

The CSV file is treated as immutable input data.

It is:

* Never modified
* Never rewritten
* Never used as persistent storage

### Why This Design

This preserves the requirement that the CSV remains unchanged while still allowing runtime persistence via database.

The CSV acts as initial state; the database acts as runtime store.

---

## 3.3 Identify People by Favourite Colour

### Implemented Solution

* `GET /persons/color/{color}`
* Case-insensitive handling
* ASCII normalization support (`gruen`, `tuerkis`, `weiss`)
* Canonical German output

All normalization logic is encapsulated inside the `FavouriteColour` value object.

### Why This Design

This guarantees:

* Centralized validation
* No string comparison logic scattered in controllers
* Strong domain consistency

---

# 4. Acceptance Criteria — Explicit Mapping

## 1. CSV Represented Internally by Model Class

✔ Implemented via:

* CSV record model
* Domain entity `Person`
* Domain value object `FavouriteColour`
* `ColourCodeMapper`

The CSV is transformed into domain objects before persistence.

---

## 2. File Access Abstracted

✔ Implemented via:

```csharp
IPersonCsvSource
```

The CSV source can be replaced without touching controllers or domain logic.

This satisfies the replaceability requirement.

---

## 3. REST Interface Implemented as Specified

✔ JSON structure matches exactly
✔ `id` equals CSV line number
✔ Proper status codes used
✔ Correct content type `application/json`

Swagger provided for verification and documentation.

---

## 4. Data Access via Dependency Injection

✔ All dependencies registered via DI:

* Repository
* CSV source
* DbContext
* Hosted seeding service

No manual instantiation inside controllers.

---

## 5. Unit Tests for REST Interface

✔ Domain unit tests
✔ Full integration tests using `WebApplicationFactory`
✔ Dedicated test database
✔ Deterministic DB reset strategy

**39 total tests — all passing.**

Tests cover:

* Value object invariants
* Mapping correctness
* HTTP response behavior
* Validation scenarios
* Filtering logic
* POST creation logic

---

## 6. CSV Not Modified

✔ File untouched
✔ No structural change
✔ Only read during startup

---

# 5. Bonus Features

## POST /persons

* Validates fields
* Validates colour via domain
* Assigns `CsvLineNumber = max + 1`
* Returns `201 Created`

Fully consistent with CSV-based ID model.

---

## Secondary Data Source (Database)

* PostgreSQL via EF Core 9
* Migrations
* Idempotent seeding
* Clean repository implementation

CSV is input.
Database is runtime persistence layer.

---

## CI-Friendly Build

* MSBuild structured solution
* Deterministic `dotnet build`
* Deterministic `dotnet test`
* Docker-based reproducibility

---

# 6. Core Design Decisions

## 6.1 Domain-First Modeling

The domain layer is completely independent.

It contains:

* `Person`
* `PersonId`
* `FavouriteColour`
* `ColourCodeMapper`

It has:

* No EF references
* No ASP.NET references
* No infrastructure dependencies

This makes business logic fully testable in isolation.

---

## 6.2 Idempotent Startup Seeding

Startup behavior:

* Existing `CsvLineNumber` → update
* Missing → insert

This guarantees:

* No duplicates
* Safe restarts
* Deterministic state

---

## 6.3 Deterministic Integration Testing

* Dedicated Docker test database
* Custom `WebApplicationFactory`
* TRUNCATE reset strategy
* Full HTTP stack tested

No dependency on development DB.

---

## 6.4 Docker-First Strategy

* Multi-stage build
* PostgreSQL container
* Environment-driven configuration
* Swagger enabled in Development
* Production-ready environment variable support

One command full stack:

```bash
docker compose up --build
```

---

# 7. Data Flow

CSV → CSV Source → Domain Model → Repository → PostgreSQL
HTTP → Controller → Application Port → Repository → Database

---

# 8. Test Summary

Total tests: **39**
Failed: **0**
Skipped: **0**

Covers:

* Domain invariants
* Mapping logic
* REST behavior
* Validation logic
* Integration across layers

---

# 9. How to Run
## Clone Repository
using your prefered method.

## Local

```bash
dotnet run --project src/Assecor.Assessment.Api
```

## Docker

```bash
docker compose up --build
```

Swagger:

```
http://localhost:8080/swagger
```

---

# Final Result

The implemented system:

* Fully satisfies all mandatory requirements
* Implements all bonus requirements
* Is strongly aligned with DDD
* Is cleanly layered
* Is fully tested (39 tests passing)
* Is fully containerised
* Is CI-ready
* Is deterministic and restart-safe

## License

This project is licensed under the MIT License.  
See the `LICENSE` file for details.

---

## Author

Kees Toukam  

📧 kees.toukam@gmail.com  
🔗 https://www.linkedin.com/in/taty-kees-petran-toukam/