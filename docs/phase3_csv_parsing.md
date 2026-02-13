# Phase 3 — CSV Parsing & Domain Mapping (Infrastructure)

## Goal

Implement CSV reading and mapping logic inside the Infrastructure layer, preparing Domain `Person` objects for later database seeding.

---

## 3.1 CsvPersonSource (Infrastructure implementation)

**Purpose:** Implement `IPersonCsvSource` and isolate all file parsing logic from Domain and API layers.

**Design intent:**

* Infrastructure reads and reconstructs logical CSV records.
* Parsing concerns (buffering, trimming, encoding, validation) stay outside Domain.
* Output of parsing is a collection of `PersonCsvRecord`.

**Acceptance criteria link:** CSV is read via an interface and represented internally by a model class.

---

### CSV characteristics to handle

* No header row.
* UTF-8 content (umlauts, emoji).
* Leading/trailing whitespace.
* Empty lines.
* One logical record split across two physical lines.
* Logical record = exactly 4 fields (3 commas).
* Third column contains combined `Zip + City`.
* Fail-fast on invalid records.

---

### Implementation steps (essential)

1. Create folder `src/Assecor.Assessment.Infrastructure/Csv/` via
   `mkdir -p src/Assecor.Assessment.Infrastructure/Csv`.
2. Create file `src/Assecor.Assessment.Infrastructure/Csv/CsvPersonSource.cs` via
   `touch src/Assecor.Assessment.Infrastructure/Csv/CsvPersonSource.cs`.
3. Implement `CsvPersonSource : IPersonCsvSource`.
4. Read file using UTF-8.
5. Ignore empty lines.
6. Buffer lines until a complete logical record (exactly 3 commas) is formed.
7. Split into 4 trimmed fields:

   * LastName
   * FirstName
   * ZipAndCity
   * ColourCode
8. Parse `ColourCode` as `int` (fail-fast if invalid).
9. Return `IReadOnlyList<PersonCsvRecord>`.
10. Build to verify compilation:
    `dotnet build src/Assecor.Assessment.Infrastructure -v minimal`.

---

## 3.2 Mapping to Domain Person

**Purpose:** Convert `PersonCsvRecord` to Domain `Person`.

**Design intent:**

* Mapping logic remains in Infrastructure.
* Domain stays free of parsing concerns.

---

### Mapping rules

* `PersonId` = logical record index (starting at 1).
* `FavouriteColour` = `ColourCodeMapper.FromCode(code)`.
* `ZipAndCity` handling:

  * Trim input.
  * Split on whitespace (`RemoveEmptyEntries`).
  * First token → `ZipCode`.
  * Remaining tokens joined → `City`.
* Fail-fast if ZipCode missing or invalid.

---

### Implementation steps (essential)

1. Implement mapping logic inside Infrastructure (separate method or service).
2. Construct `Person` using:

   * `PersonId.From(index)`
   * `FirstName`
   * `LastName`
   * `ZipCode`
   * `City`
   * `FavouriteColour`
3. Return `IReadOnlyList<Person>`.
4. Build to verify compilation:
   `dotnet build src/Assecor.Assessment.Infrastructure -v minimal`.

---