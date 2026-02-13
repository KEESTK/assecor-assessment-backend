# Phase 2 — Domain + Contracts (DDD)

## Goal
Establish the Domain model and the Application-layer contracts (ports) that satisfy:
- Explicit domain modelling (Person, PersonId, FavouriteColour)
- Canonical German colour names + mapping from CSV colour code (1–7)
- Abstractions for file access and data access (interfaces)
- Clean Architecture direction of dependencies:
  Domain <- Application <- Infrastructure <- API

---

## 1) Domain model

### 1.1 PersonId (CSV line number)
**Purpose:** Public API `id` is the CSV line number. Domain uses `PersonId` to make that explicit and avoid mixing IDs.

**Acceptance criteria link:** The ID logic is clear and consistent across CSV import and API responses.

#### Implementation steps (essential)
1. Create folder `src/Assecor.Assessment.Domain/Persons/` via `mkdir -p src/Assecor.Assessment.Domain/Persons`.
2. Create file `src/Assecor.Assessment.Domain/Persons/PersonId.cs` via `touch src/Assecor.Assessment.Domain/Persons/PersonId.cs` and add `PersonId.cs` as a value object wrapping `int`.
3. Enforce invariant: `value > 0` (throw `ArgumentOutOfRangeException`).
4. Provide a single creation method (constructor or `From(int)`).
5. Use `PersonId` in the `Person` model (next sub-step).
6. Build to verify compilation: `dotnet build src/Assecor.Assessment.Domain -v minimal`

---

### 1.2 FavouriteColour (canonical German names)
**Purpose:** Represent allowed colours as a value object (normalized and validated).

**Allowed colours (canonical):**
- blau
- grün
- violett
- rot
- gelb
- türkis
- weiß

#### Implementation steps (essential)

1. Create folder `src/Assecor.Assessment.Domain/Colours/` via `mkdir -p src/Assecor.Assessment.Domain/Colours`.
2. Create file `src/Assecor.Assessment.Domain/Colours/FavouriteColour.cs` via `touch src/Assecor.Assessment.Domain/Colours/FavouriteColour.cs` and add `FavouriteColour.cs` as a value object wrapping a canonical `string` (`Value`).
3. Enforce invariant: input must not be null, empty, or whitespace (throw `ArgumentException`).
4. Provide a single creation method `From(string)` responsible for:

   * trimming whitespace,
   * normalizing to lowercase,
   * mapping ASCII variants (`gruen`, `tuerkis`, `weiss`) to canonical German names.
5. Validate normalized value against the allowed canonical set:
   `blau, grün, violett, rot, gelb, türkis, weiß`
   and reject unknown colours (throw `ArgumentException`).
6. Ensure canonical form is stored internally (lowercase German spelling).
7. Build to verify compilation: `dotnet build src/Assecor.Assessment.Domain -v minimal`.


### 1.3 Colour mapping (CSV code -> FavouriteColour)
**Purpose:** Map CSV first column values to canonical colour.

**Mapping:**
1 -> blau  
2 -> grün  
3 -> violett  
4 -> rot  
5 -> gelb  
6 -> türkis  
7 -> weiß  

#### Implementation steps (essential)

1. Create file `src/Assecor.Assessment.Domain/Colours/ColourCodeMapper.cs` via `touch src/Assecor.Assessment.Domain/Colours/ColourCodeMapper.cs` and add `ColourCodeMapper` as a static mapper for CSV colour codes.
2. Implement a single mapping method (e.g. `FromCode(int code)`) that returns `FavouriteColour` using the required mapping:

   * `1 -> blau`, `2 -> grün`, `3 -> violett`, `4 -> rot`, `5 -> gelb`, `6 -> türkis`, `7 -> weiß`.
3. Throw `ArgumentOutOfRangeException` for unsupported codes.
4. Build to verify compilation: `dotnet build src/Assecor.Assessment.Domain -v minimal`.

---

### 1.4 Person entity
**Fields (as required by API output):**
- id (PersonId)
- name (first name)
- lastname
- zipcode
- city
- color (FavouriteColour)

#### Implementation steps (essential)

1. Create file `src/Assecor.Assessment.Domain/Persons/Person.cs` via `touch src/Assecor.Assessment.Domain/Persons/Person.cs`.
2. Implement `Person` as an immutable domain model (record) using:

   * `PersonId Id`
   * `string FirstName`
   * `string LastName`
   * `string ZipCode`
   * `string City`
   * `FavouriteColour Colour`
3. Keep field naming aligned with API requirements ("should / will" be maped to `name` / `lastname` in the API layer later).
4. Build to verify compilation: `dotnet build src/Assecor.Assessment.Domain -v minimal`.


---

## 2) Application ports (contracts)

### 2.1 IPersonRepository (data access port)
**Purpose:** Abstract access to persons so that data source can be swapped (CSV-only, EF Core DB, etc.) without changing callers.

**Required operations (aligned with endpoints):**
- Get all persons
- Get person by id
- Get persons by colour
- Add person (for POST bonus)

#### Implementation steps (essential)

1. Create folder `src/Assecor.Assessment.Application/Ports/` via `mkdir -p src/Assecor.Assessment.Application/Ports`.
2. Create file `src/Assecor.Assessment.Application/Ports/IPersonRepository.cs` via `touch src/Assecor.Assessment.Application/Ports/IPersonRepository.cs`.
3. Define `IPersonRepository` with async methods aligned with the required endpoints:

   * `GetAllAsync()`
   * `GetByIdAsync(PersonId id)`
   * `GetByColourAsync(FavouriteColour colour)`
   * `AddAsync(Person person)` (for `POST /persons`)
4. Use Domain types in method signatures (`Person`, `PersonId`, `FavouriteColour`) to keep the contract domain-centric.
5. Build to verify compilation: `dotnet build src/Assecor.Assessment.Application -v minimal`.


---

### 2.2 IPersonCsvSource (file access port)

**Purpose:** Abstract CSV reading behind an interface.

**Design intent:**
PersonCsvRecord represents a raw logical CSV record and mirrors the file structure.
IPersonCsvSource defines the contract for reading CSV data, while the Infrastructure layer provides the concrete parsing implementation.
This isolates file parsing concerns (buffering, trimming, encoding, validation) from Domain and API layers.
**Acceptance criteria link:** “File access is done with an interface.”

**CSV characteristics to handle:**

* No header row.
* UTF-8 content (umlauts, emoji).
* Leading/trailing whitespace.
* Empty lines.
* One record split across two lines (must buffer until 4 fields / 3 commas).
* Third column contains combined `Zip + City`.
* Fail-fast on invalid records (deterministic behavior).

---

#### Implementation steps (essential)

1. Create file `src/Assecor.Assessment.Application/Ports/PersonCsvRecord.cs` via
   `touch src/Assecor.Assessment.Application/Ports/PersonCsvRecord.cs`.
2. Define `PersonCsvRecord` with:

   * `string LastName`
   * `string FirstName`
   * `string ZipAndCity`
   * `int ColourCode`
3. Create file `src/Assecor.Assessment.Application/Ports/IPersonCsvSource.cs` via
   `touch src/Assecor.Assessment.Application/Ports/IPersonCsvSource.cs`.
4. Define method:
   `Task<IReadOnlyList<PersonCsvRecord>> ReadAllAsync(CancellationToken ct = default);`
5. Build to verify compilation:
   `dotnet build src/Assecor.Assessment.Application -v minimal`.

---

## 3) Verification checklist
After implementing Phase 2 code. Tests were still empty a this phase :-) :

```bash
dotnet build
dotnet testn
```