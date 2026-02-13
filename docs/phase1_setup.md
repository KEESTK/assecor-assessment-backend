
## Create `phase1_setup.md`

```bash
touch docs/phase1_setup.md
code phase1_setup.md   # or: nano phase1_setup.md
```

````md
# Phase 1 — Solution & Project Setup 

From the repository root (where README.md and sample-input.csv exist).

## 0) Verify prerequisites

```bash
docker --version
dotnet --version
node --version
````

## 1) Create folders

```bash
mkdir -p src tests
```

## 2) Create solution

```bash
dotnet new sln -n Assecor.Assessment
```

Expected output: creates `Assecor.Assessment.sln` in repo root.

## 3) Create projects

### Domain

```bash
dotnet new classlib -n Assecor.Assessment.Domain -o src/Assecor.Assessment.Domain
```

### Application

```bash
dotnet new classlib -n Assecor.Assessment.Application -o src/Assecor.Assessment.Application
```

### Infrastructure

```bash
dotnet new classlib -n Assecor.Assessment.Infrastructure -o src/Assecor.Assessment.Infrastructure
```

### API

```bash
dotnet new webapi -n Assecor.Assessment.Api -o src/Assecor.Assessment.Api
```

### Tests

```bash
dotnet new xunit -n Assecor.Assessment.Tests -o tests/Assecor.Assessment.Tests
```

## 4) Add projects to solution

```bash
dotnet sln Assecor.Assessment.sln add \
  src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj \
  src/Assecor.Assessment.Application/Assecor.Assessment.Application.csproj \
  src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj \
  src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj \
  tests/Assecor.Assessment.Tests/Assecor.Assessment.Tests.csproj
```

## 5) Add project references (Domain ← Application ← Infrastructure ← Api)

### Application -> Domain

```bash
dotnet add src/Assecor.Assessment.Application/Assecor.Assessment.Application.csproj reference \
  src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj
```

### Infrastructure -> Application + Domain

```bash
dotnet add src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj reference \
  src/Assecor.Assessment.Application/Assecor.Assessment.Application.csproj \
  src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj
```

### Api -> Application + Infrastructure + Domain

```bash
dotnet add src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj reference \
  src/Assecor.Assessment.Application/Assecor.Assessment.Application.csproj \
  src/Assecor.Assessment.Infrastructure/Assecor.Assessment.Infrastructure.csproj \
  src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj
```

### Tests -> Api + Domain (starter set)

```bash
dotnet add tests/Assecor.Assessment.Tests/Assecor.Assessment.Tests.csproj reference \
  src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj \
  src/Assecor.Assessment.Domain/Assecor.Assessment.Domain.csproj
```

## 6) Verify build + tests

```bash
dotnet restore
dotnet build
dotnet test
```

## 7) Useful commands

Run API locally:

```bash
dotnet run --project src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj
```

## 8) SAMPLE OUTPUT phase 6-7:

```bash
dotnet build
Restore complete (0,5s)
  Assecor.Assessment.Domain succeeded (15,7s) → src/Assecor.Assessment.Domain/bin/Debug/net9.0/Assecor.Assessment.Domain.dll
  Assecor.Assessment.Application succeeded (6,3s) → src/Assecor.Assessment.Application/bin/Debug/net9.0/Assecor.Assessment.Application.dll
  Assecor.Assessment.Infrastructure succeeded (11,5s) → src/Assecor.Assessment.Infrastructure/bin/Debug/net9.0/Assecor.Assessment.Infrastructure.dll
  Assecor.Assessment.Api succeeded (11,8s) → src/Assecor.Assessment.Api/bin/Debug/net9.0/Assecor.Assessment.Api.dll
  Assecor.Assessment.Tests succeeded (0,7s) → tests/Assecor.Assessment.Tests/bin/Debug/net9.0/Assecor.Assessment.Tests.dll

Build succeeded in 46,8s
dotnet run --project src/Assecor.Assessment.Api/Assecor.Assessment.Api.csproj
Using launch settings from src/Assecor.Assessment.Api/Properties/launchSettings.json...
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5183
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /.../assecor-assessment-backend/src/Assecor.Assessment.Api
```


````
