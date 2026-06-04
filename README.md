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

### `GET|POST /auth/sign-in`

OpenIddict authorization endpoint with a hosted Razor Pages + Bootstrap sign-in
UI for authorization code issuance.

Responses:

- `200 OK` with the hosted sign-in UI for valid authorization requests.
- `400 Bad Request` with a hosted UI message listing missing/invalid authorization parameters.
- Redirect responses for authorization flow completion/errors per OAuth/OIDC.

### `POST /auth/token`

OpenIddict token endpoint for:

- `grant_type=authorization_code`
- `grant_type=refresh_token`

## Configuration

The service requires `ConnectionStrings:IdentityServiceDb` at runtime. Keep real
connection strings out of source control and provide them through environment
variables, user secrets, or local-only configuration.

## Authorization Code + PKCE Flow

`/auth/sign-in` is the authorization endpoint and `/auth/token` is the token
exchange endpoint. Password grant is not supported.

Before testing the flow, register a local OpenIddict client using:

```text
code/backend/samples/identity-service/Seed OpenIddict Client.sql
```

### 1) Authorization Request

Open this URL in a browser:

```text
https://localhost:15200/auth/sign-in?client_id=1708699d-f872-463b-a255-bda71e97a265&redirect_uri=https%3A%2F%2Flocalhost%3A4200%2Fauth%2Fcallback&response_type=code&response_mode=query&scope=openid%20profile%20email%20roles%20offline_access&state=test-state-123&nonce=test-nonce-123&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256
```

Example PKCE verifier/challenge pair used above:

```text
code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
code_challenge_method=S256
```

### 2) Exchange Authorization Code

Copy the fresh `code` query value from the redirect after each successful
hosted sign-in. Authorization codes are single-use and are bound to the
`client_id`, `redirect_uri`, and PKCE verifier used by the authorization
request.

```bash
curl -X POST "https://localhost:15200/auth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code&client_id=1708699d-f872-463b-a255-bda71e97a265&code=<authorization_code>&redirect_uri=https%3A%2F%2Flocalhost%3A4200%2Fauth%2Fcallback&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
```

### 3) Refresh Token Exchange

```bash
curl -X POST "https://localhost:15200/auth/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&client_id=1708699d-f872-463b-a255-bda71e97a265&refresh_token=<refresh_token>"
```

## Bruno Samples

Use the shared backend Bruno collection at `code/backend/samples/identity-service/`.

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
