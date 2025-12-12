# SimpleJira (Aspire + Blazor Server + Postgres)

Lightweight Jira-like demo built with .NET 8, ASP.NET Core, EF Core, Blazor Server, and .NET Aspire service defaults. This README is a practical runbook: prerequisites, how to run locally, migrations, auth, and common workflows (seed users, create projects/issues, tests, CI).

---

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- ASP.NET workload + Aspire:
  ```bash
  dotnet workload restore
  # or, if needed:
  dotnet workload install aspire
  ```
- PostgreSQL 15+ (local or via Aspire/Docker)
- Optional: Docker/Docker Compose (if not using Aspire’s built-in Postgres)
- Node is **not** required (Blazor Server).

Connection string in `appsettings.Development.json` is for a manually run Postgres. When using Aspire (`SimpleJira.AppHost`), a Postgres container is created for you with a generated password/port; see the “Running with Aspire” section to retrieve it.

---

## Projects
- `SimpleJira.ApiService` – REST API (projects/issues/users/categories), EF Core/Postgres, JWT auth, Serilog.
- `SimpleJira.Web` – Blazor Server UI (projects/board/login).
- `SimpleJira.Contracts` – DTOs and enums shared by API/UI.
- `SimpleJira.ApiService.Tests` – API unit tests (controllers, validation, links/comments, auth).
- `SimpleJira.Web.Tests` – bUnit UI tests (login, project list/dialog, issue board, users page).
- `.github/workflows/ci.yml` – CI with build/test/migrations check against ephemeral Postgres.

---

## Running locally (two options)

### A) Using Aspire (recommended for local dev)
1) Run the AppHost (creates Postgres, API, Web, dashboard):
   ```bash
   dotnet run --project SimpleJira.AppHost
   ```
   - The Aspire dashboard URL is printed to the console (e.g., http://localhost:15xxx).
   - Open the dashboard → Resources → `postgres` to see the generated connection string, host, port, username, and password.

2) Apply migrations against the Aspire Postgres (copy the connection string from the dashboard):
   ```bash
   dotnet tool install -g dotnet-ef   # if not already
   CONNECTION_STRING="Host=localhost;Port=<port>;Database=simplejira;Username=postgres;Password=<password>"
   dotnet ef database update --project SimpleJira.ApiService --startup-project SimpleJira.ApiService --connection "$CONNECTION_STRING"
   ```

3) Use the Web UI at the endpoint shown in the dashboard (e.g., http://localhost:5xxx). Login at `/login` with any username to obtain a dev JWT; you’ll be redirected to Projects with the token applied automatically.

### B) Manual run (if you don’t want Aspire)
1) Start Postgres yourself (example with Docker):
   ```bash
   docker run --name simplejira-db -p 57408:5432 \
     -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=simplejira \
     -d postgres:15
   ```
   Update the connection string in `SimpleJira.ApiService/appsettings.Development.json` if you change port/user/pass.

2) Apply migrations:
   ```bash
   dotnet tool install -g dotnet-ef   # if not already
   dotnet ef database update --project SimpleJira.ApiService --startup-project SimpleJira.ApiService
   ```

3) Run API and Web (separate terminals):
   ```bash
   dotnet run --project SimpleJira.ApiService
   dotnet run --project SimpleJira.Web
   ```

4) Login at `/login` with any username to obtain a dev JWT and start using Projects/Issues.

---

## EF Core & Migrations
- DbContext: `SimpleJira.ApiService/Data/JiraDbContext.cs`
- Migrations folder: `SimpleJira.ApiService/Migrations`
- To add a migration:
  ```bash
  dotnet ef migrations add <Name> \
    --project SimpleJira.ApiService --startup-project SimpleJira.ApiService
  ```
- To apply:
  ```bash
  dotnet ef database update --project SimpleJira.ApiService --startup-project SimpleJira.ApiService
  ```
- CI checks pending migrations against an ephemeral Postgres and fails if any are pending.

---

## Auth
- Dev-only JWT issuer at `POST /auth/token` (body: `{ "username": "<name>" }`).
- JWT settings in `SimpleJira.ApiService/appsettings.Development.json` (`Jwt:Issuer`, `Audience`, `Key`).
- In production, move secrets to environment/KeyVault and restrict CORS to your web origin.

---

## Seed data
On first run, migrations seed:
- Categories: Software, Business, Service Desk
- Users: Jane Product (`bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1`), John Developer (`bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2`)

You can create additional users via API (`/users`) or database directly.

---

## Common workflows
- **Create a project**: UI “Create project” dialog or `POST /projects` with `CreateProjectRequest` (name/key/type/avatar/category/lead).
- **Create an issue**: On a project board → “New issue” modal, or `POST /projects/{projectId}/issues` with title/summary/storyPoints/assignee/reporters/links.
- **Drag/drop**: Board supports Todo/InProgress/Done transitions; drops call `PATCH /issues/{id}/status`.
- **Assign**: Click avatar chip → choose user; calls `PATCH /issues/{id}/assignee`.
- **Comments/Links**: `POST /issues/{id}/comments`, `POST /issues/{id}/links`.

---

## Logging & error handling
- Serilog with request logging; production config outputs compact JSON (`appsettings.Production.json`).
- ProblemDetails with `/error` endpoint; stack traces hidden outside Development.
- Frontend displays inline errors/toasts for most operations (create/update/drag/assign).

---

## Testing
- Unit/UI tests:
  ```bash
  dotnet test SimpleJira.sln
  ```
  - API: validation, links/comments, status/assignee updates, auth token issuance.
  - Web (bUnit): login, project list, create project dialog, issue board interactions, issue creation modal, users page.

---

## CI
GitHub Actions (`.github/workflows/ci.yml`):
- Restore, format check, build, test.
- Spins up Postgres service, applies migrations, fails on pending migrations.
- Uses .NET 8 SDK.

---

## Deployment notes
- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Provide Postgres connection string via env vars/config provider.
- Set a strong `Jwt:Key` and restrict `Cors:AllowedOrigins`.
- Enable HTTPS, HSTS (already wired for non-dev).
- Consider external sinks for Serilog (Seq/Elastic) in prod.

---

## Deploying with Aspire (to a host or container)
1) **Ensure Aspire workload is installed**:
   ```bash
   dotnet workload restore
   ```
2) **Publish the AppHost** (this builds the referenced projects and collects the distributed application assets):
   ```bash
   dotnet publish SimpleJira.AppHost -c Release -o out/apphost
   ```
3) **Run the published AppHost** on your target server/VM/container:
   ```bash
   ./out/apphost/SimpleJira.AppHost
   ```
   - This will spin up the API, Web, Postgres container, and the Aspire dashboard. The dashboard URL and generated Postgres connection info are printed to the console.
   - For production, set env vars (connection string, JWT key, CORS origins) before launching. Example:
     ```bash
     export ConnectionStrings__simplejira="Host=...;Port=...;Database=simplejira;Username=postgres;Password=..."
     export Jwt__Key="your-strong-32char-key"
     export Cors__AllowedOrigins="https://your-domain"
     export ASPNETCORE_ENVIRONMENT=Production
     ```
4) **Apply migrations** once against the running Postgres (connection string from the dashboard or env var):
   ```bash
   dotnet tool install -g dotnet-ef
   dotnet ef database update --project SimpleJira.ApiService --startup-project SimpleJira.ApiService --connection "$ConnectionStrings__simplejira"
   ```
5) **Swagger from Aspire dashboard**: in the Aspire dashboard, open the `apiservice` resource, then click the exposed HTTP endpoint; append `/swagger` to reach the OpenAPI UI (e.g., `http://localhost:5xxx/swagger`). Use that to explore/try endpoints.

6) **Front the Web/API endpoints with your reverse proxy** (NGINX/Traefik) for HTTPS, HSTS, and security headers. Allow WebSockets for Blazor Server.

Notes:
- The Aspire dashboard is great for dev/staging. For production, lock it down or disable it.
- You can containerize the published AppHost if desired; ensure Docker is available on the target host.
- If you prefer managed Postgres, override the Aspire-generated connection string with your managed DB via env vars and skip the built-in Postgres resource.

---

## Troubleshooting
- **JWT key too short**: ensure `Jwt:Key` is at least 32 chars (256 bits).
- **Migrations errors**: drop/recreate DB in dev or rerun `dotnet ef database update`.
- **CORS/401**: ensure login issued a token and the Web client applied the bearer (AuthService holds it in-memory).
