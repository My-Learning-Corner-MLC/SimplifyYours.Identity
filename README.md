# Identity Service

Backend authorization service for Simplify Yours identity and authentication capabilities.

## Current API

### `GET /ping`

Returns an Identity Service up message and the current UTC date-time.

### `POST /auth/sign-up`

Creates a normal user account.

Request body:

```json
{
  "fullName": "Example User",
  "email": "user@example.com",
  "password": "UseARealPasswordHere",
  "confirmPassword": "UseARealPasswordHere",
  "acceptTermsAndPrivacy": true
}
```

Responses:

- `201 Created` with the created user identity summary.
- `400 Bad Request` with authentication errors when sign-up fails.

### `POST /auth/sign-in`

OpenIddict token endpoint for password sign-in.

Responses:

- `200 OK` with the OpenIddict token response when credentials are valid.
- `403 Forbidden` with OpenIddict `invalid_grant` details when the grant type or credentials are invalid.

## Configuration

The service requires `ConnectionStrings:IdentityServiceDb` at runtime. Keep real
connection strings out of source control and provide them through environment
variables, user secrets, or local-only configuration.

## Sign-In Token Request

`POST /auth/sign-in` uses OpenIddict token endpoint handling. Submit credentials
as form data:

```text
grant_type=password
username=user@example.com
password=<password>
scope=email profile roles offline_access
```

## Developer Commands

Run these commands from `code/backend/identity-service/`.

### Restore

```bash
dotnet restore IdentityService.sln
```

### Build

```bash
dotnet build IdentityService.sln --configuration Release --no-restore
```

### Test

```bash
dotnet test IdentityService.sln --configuration Release --no-build
```

### Test With Coverage

```bash
dotnet test IdentityService.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

### Run The API Locally

```bash
ConnectionStrings__IdentityServiceDb="Host=localhost;Port=54321;Database=SimplifyYours.Identities;Username=postgres;Password=<password>" \
dotnet run --project src/IdentityService.Api/IdentityService.Api.csproj
```

### Install EF CLI

Install the EF CLI once if `dotnet ef` is not available.

```bash
dotnet tool install --global dotnet-ef --version 8.*
```

### Add A Migration

```bash
dotnet ef migrations add <MigrationName> \
  --project src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj \
  --startup-project src/IdentityService.Api/IdentityService.Api.csproj \
  --context IdentityServiceDbContext \
  --output-dir Persistence/Migrations
```

### Apply Migrations

```bash
dotnet ef database update \
  --project src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj \
  --startup-project src/IdentityService.Api/IdentityService.Api.csproj \
  --context IdentityServiceDbContext
```

### List Migrations

```bash
dotnet ef migrations list \
  --project src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj \
  --startup-project src/IdentityService.Api/IdentityService.Api.csproj \
  --context IdentityServiceDbContext
```

## README Maintenance

Keep this README up to date during development. When a feature introduces a new
endpoint, configuration value, migration workflow, local dependency, test
command, script, or operational command, add or update the relevant README
section in the same change.

## CI Checks

```bash
dotnet restore IdentityService.sln
dotnet build IdentityService.sln --configuration Release --no-restore
dotnet test IdentityService.sln --configuration Release --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```
