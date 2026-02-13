
# Phase 6 — REST API Endpoints

## Goal

Implement the required REST interface according to the specification.

This fulfills the acceptance criterion:

> “The REST interface is implemented according to the above specifications.”

---

## 6.1 Required Endpoints

### `GET /persons`

* Returns all persons.
* Response type: `application/json`.
* Status code: `200 OK`.

---

### `GET /persons/{id}`

* Returns person by `CsvLineNumber`.
* Status code:

  * `200 OK` if found
  * `404 Not Found` if missing

---

### `GET /persons/color/{color}`

* Returns persons with the specified favourite colour.
* Input is case-insensitive.
* Internally normalized to canonical colour representation.
* Status code: `200 OK`.

---

## 6.2 Bonus Endpoint

### `POST /persons`

* Validates required fields:

  * `name`
  * `lastname`
  * `zipcode`
  * `city`
  * `color`
* Validates allowed colours via `FavouriteColour`.
* Assigns new `CsvLineNumber = max(existing) + 1`.
* Persists using `IPersonRepository`.
* Status code:

  * `201 Created` on success
  * `400 Bad Request` on validation failure

---

## 6.3 Implementation Requirements

* Implement dedicated `PersonsController`.
* Use DTOs for request/response.
* Do not expose EF entities.
* Controller depends only on `IPersonRepository`.
* No direct EF access inside controller.
* Use proper HTTP status codes (`200`, `201`, `400`, `404`).

---

## 6.4 API Documentation (Required)

* Enable Swagger/OpenAPI.
* Expose OpenAPI UI in Development environment.
* Ensure response schemas match specification.
* Provide example request/response for `POST /persons`.

This ensures:

* Discoverable API
* Self-documenting contract
* Easy verification during assessment review

---

## Implementation Steps (essential)
1. Add required packages for swager api
```bash
dotnet add src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj package Swashbuckle.AspNetCore --version 10.1.2 
dotnet restore src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj
```
    Enable Swagger in `Program.cs`.
```csharp
//using directives
//...

var builder = WebApplication.CreateBuilder(args);
// Register controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
//...

// CSV source (copied to output directory by MSBuild)
//...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Middleware
//...
```

2. Create DTOS (Contracts) according to specification file.
```bash
mkdir -p src/Assecor.Assessment.Api/Contracts
touch src/Assecor.Assessment.Api/Contracts/PersonResponse.cs
touch src/Assecor.Assessment.Api/Contracts/CreatePersonRequest.cs
```
3. Implement PersonsController
```bash
mkdir -p src/Assecor.Assessment.Api/Controllers
touch src/Assecor.Assessment.Api/Controllers/PersonsController.cs
```
   Implement all endpoints (3 GETs + POST) according to specification.

---
## 6.3 Controller Behavior Explanation

The `PersonsController` exposes the REST interface defined in the specification and delegates all data access to `IPersonRepository`.

### Architectural Role

* The controller belongs to the **API layer**.
* It depends only on the **Application port (`IPersonRepository`)**.
* It does **not** access EF Core or the database directly.
* It converts between:

  * Domain model (`Person`)
  * API DTOs (`PersonResponse`, `CreatePersonRequest`)

This preserves clean architecture boundaries.

---

### Endpoint Behavior

#### `GET /persons`

* Calls `GetAllAsync`.
* Maps domain entities to response DTOs.
* Returns `200 OK`.

---

#### `GET /persons/{id}`

* Validates `id > 0`.
* Calls `GetByIdAsync`.
* Returns:

  * `200 OK` if found
  * `404 Not Found` if missing

---

#### `GET /persons/color/{color}`

* Normalizes input via `FavouriteColour.From(color)`.
* Rejects unsupported colours (`400 Bad Request`).
* Calls `GetByColourAsync`.
* Returns filtered list (`200 OK`).

---

#### `POST /persons`

* Validates required fields.
* Validates colour using domain value object.
* Computes next `CsvLineNumber = max + 1`.
* Creates a new `Person` domain entity.
* Persists via repository.
* Returns `201 Created` with `Location` header.

This fulfills the bonus requirement while maintaining consistency with the CSV-based ID model.

---

### Status Code Strategy

* `200 OK` → successful read
* `201 Created` → successful creation
* `400 Bad Request` → validation failure
* `404 Not Found` → missing resource

---

4. Build to verify compilation:

```bash
dotnet build -v minimal
```

5. Set up Postgres via Docker Compose (repo root):

```bash
touch docker-compose.yml
```

Add a `postgres` service (Postgres 16, port `5432:5432`, db `assecor`, user/pass `postgres/postgres`, named volume), then start it:

```bash
docker compose up -d
```

Apply migrations:

```bash
dotnet ef database update \
  --project src/Assecor.Assessment.Infrastructure \
  --startup-project src/Assecor.Assessment.Api
```
Expected "first" migration output:
```bash
dotnet ef database update \
>   --project src/Assecor.Assessment.Infrastructure \
>   --startup-project src/Assecor.Assessment.Api
Build started...
Build succeeded.
Failed executing DbCommand (14ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId";
Acquiring an exclusive lock for migration application. See https://aka.ms/efcore-docs-migrations-lock for more information if this takes too long.
Applying migration '20260212224418_InitialCreate'.
Done.
```

Post migration verification:

Run:
```bash
docker compose ps
docker exec -it assecor-postgres psql -U postgres -d assecor -c "\dt"
```
You should see the Persons table plus __EFMigrationsHistory.
```xml

                 List of relations
 Schema |         Name          | Type  |  Owner   
--------+-----------------------+-------+----------
 public | Persons               | table | postgres
 public | __EFMigrationsHistory | table | postgres
(2 rows)
```

6. Run (quick check)

```bash
dotnet run --project src/Assecor.Assessment.Api
```

Open Swagger UI (Development only):

```http
http://localhost:<port>/swagger
```
In my case it was listening on http not https yet, at this stage

```http
http://localhost:5183/swagger
```
7. Run (Curl checks)
```bash
curl http://localhost:5183/persons
curl http://localhost:5183/persons/1
curl http://localhost:5183/persons/color/gruen

curl http://localhost:5183/persons/8  && curl http://localhost:5183/persons/9 #(check if the output is complete with zipcode and city. Expecting line 8-9 (as id: 8) and 10 (as id:9) from csv file as represented outputs)
curl http://localhost:5183/persons/color/rot #(check if a city with spaces is represented properly)

```
(Expected) Output at this Stage:
```bash
$ curl http://localhost:5183/persons
[
    {"id":1,"name":"Hans","lastname":"Müller","zipcode":"67742","city":"Lauterecken","color":"blau"},{"id":2,"name":"Peter","lastname":"Petersen","zipcode":"18439","city":"Stralsund","color":"grün"},{"id":3,"name":"Johnny","lastname":"Johnson","zipcode":"88888","city":"made up","color":"violett"},{"id":4,"name":"Milly","lastname":"Millenium","zipcode":"77777","city":"made up too","color":"rot"},{"id":5,"name":"Jonas","lastname":"Müller","zipcode":"32323","city":"Hansstadt","color":"gelb"},{"id":6,"name":"Tastatur","lastname":"Fujitsu","zipcode":"42342","city":"Japan","color":"türkis"},{"id":7,"name":"Anders","lastname":"Andersson","zipcode":"32132","city":"Schweden - ☀","color":"grün"},{"id":8,"name":"Bertram","lastname":"Bart","zipcode":"12313","city":"Wasweißich","color":"blau"},{"id":9,"name":"Gerda","lastname":"Gerber","zipcode":"76535","city":"Woanders","color":"violett"},{"id":10,"name":"Klaus","lastname":"Klaussen","zipcode":"43246","city":"Hierach","color":"grün"}
]
$ curl  http://localhost:5183/persons/1
{"id":1,"name":"Hans","lastname":"Müller","zipcode":"67742","city":"Lauterecken","color":"blau"}
$ curl http://localhost:5183/persons/color/gruen
[
    {"id":2,"name":"Peter","lastname":"Petersen","zipcode":"18439","city":"Stralsund","color":"grün"},{"id":7,"name":"Anders","lastname":"Andersson","zipcode":"32132","city":"Schweden - ☀","color":"grün"},{"id":10,"name":"Klaus","lastname":"Klaussen","zipcode":"43246","city":"Hierach","color":"grün"}
]

$ curl http://localhost:5183/persons/8  && curl http://localhost:5183/persons/9 #(check if the output is complete with zipcode and city. Expecting line 8-9 (as id: 8) and 10 (as id:9) from csv file as represented outputs)
{"id":8,"name":"Bertram","lastname":"Bart","zipcode":"12313","city":"Wasweißich","color":"blau"}
{"id":9,"name":"Gerda","lastname":"Gerber","zipcode":"76535","city":"Woanders","color":"violett"}
$ curl http://localhost:5183/persons/color/rot #(check if a city with spaces is represented properly)
[
    {"id":4,"name":"Milly","lastname":"Millenium","zipcode":"77777","city":"made up too","color":"rot"}
]

```




---

