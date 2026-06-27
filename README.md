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

Token issuing also requires:

- `Auth:Issuer`: stable Identity Service issuer URL, for example `https://localhost:15200/`.
- `Auth:AccessTokenEncryptionKeyBase64`: optional base64-encoded shared access-token encryption key.

When `Auth:AccessTokenEncryptionKeyBase64` is set, resource services must use
the same value for OpenIddict validation. When it is empty, Identity falls back
to development encryption credentials, which are only suitable for local
experimentation and cannot be used as a production resource-service validation
contract.

## Tenant And Permission Model

Identity owns the tenant boundary for all resource services.

- Every user belongs to exactly one tenant. The tenant is created during
  self-serve sign-up and the user is the sole member with the full permission
  catalog. There is no shared/multi-user tenant flow yet.
- Permissions are capability strings (not roles). The fixed catalog lives in
  `IdentityService.Domain.Identity.Permissions`:
  - `events.create`
  - `events.view`
  - `events.update`
  - `guests.add`
  - `tenant.manage_users`
- The `TenantAdmin` role is assigned to the user created during self-serve sign-up.
  Resource services authorize on permissions, not roles.

Sign-up creates the tenant, user, role assignment, and permission rows in a
single EF Core transaction so a partial signup cannot leak orphaned tenants or
permissionless users.

### Token Claims

Access tokens issued by `/auth/token` carry:

- `sub`: user id (Guid string)
- `email`, `name`: user identity
- `tenant_id`: single Guid string for the user's tenant
- `role`: one claim per role (currently always `TenantAdmin`)
- `permissions`: one claim per permission. Resource services should treat
  `permissions` as a set and authorize each request against the relevant
  capability string.

## Local Observability

Start shared infrastructure before running the API:

```bash
docker compose --env-file ../../infra/shared-infrastructure/infrastructure/.env -f ../../infra/shared-infrastructure/infrastructure/docker-compose.yml up -d --remove-orphans
```

The API launch profiles export logs, traces, and metrics to the local Aspire
Dashboard:

```text
OTEL_SERVICE_NAME=identity-service
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_EXPORTER_OTLP_HEADERS=x-otlp-api-key=<SIMPLIFYYOURS_ASPIRE_OTLP_API_KEY>
OTEL_RESOURCE_ATTRIBUTES=service.namespace=SimplifyYours,deployment.environment=local
```

Set `OTEL_EXPORTER_OTLP_HEADERS` in your shell before running the service. The
value must match `SIMPLIFYYOURS_ASPIRE_OTLP_API_KEY` from the shared
infrastructure `infrastructure/.env` file.

Open `http://localhost:18888` and use the token from
`docker container logs simplify-yours-aspire-dashboard`.

Do not log request bodies, response bodies, passwords, tokens, authorization
codes, refresh tokens, payment data, customer data, or unnecessary personal
data. Prefer safe context such as operation name, event ID, correlation ID,
causation ID, status, elapsed time, and attempt count.

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
