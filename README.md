# Identity Service

Backend authorization service for Simplify Yours identity and authentication capabilities.

## Current API

- `GET /ping` returns an Identity Service up message and the current UTC date-time.

## Local Checks

```bash
dotnet restore IdentityService.sln
dotnet build IdentityService.sln --configuration Release --no-restore
dotnet test IdentityService.sln --configuration Release --no-build
```
