# Outrally

Outrally is a web app that makes it easier to organize outdoor activities like hikes, trips and group events, and keeps safety information available offline.

It's built first for hiking groups in **El Paso, TX**, but it's **multi-tenant**: many groups can use the same deployment, and each group sees only its own data.

---

## Why Outrally

Hiking groups usually organize through group chats, spreadsheets and word of mouth. That works until it doesn't:

- **Details get lost.** The trailhead, start time and gear list end up buried under 200 chat messages.
- **Headcount is guesswork.** Nobody knows who's actually coming, and some trails or permits have participant limits.
- **There's no signal on the trail.** The place you most need safety information (emergency contacts, route notes, bail-out points) is also the place you can't load a web page.

Outrally gives each activity one source of truth: what, when, where and who. Safety information is built to work offline. The app is also a teaching project, so every phase is documented here for junior developers to follow.

---

## Architecture

```mermaid
flowchart LR
    subgraph Client
        SPA["Angular 22 SPA<br/>(web/)<br/>Hosted on SmarterASP.NET"]
    end

    subgraph Server["Azure App Service"]
        API["Outrally.Api<br/>Controllers, Middleware, Program.cs"]
        INFRA["Outrally.Infrastructure<br/>AppDbContext, EF Core, Migrations"]
        CORE["Outrally.Core<br/>Domain Models, Enums"]
    end

    DB[("SQL Server<br/>SmarterASP.NET")]
    MEM[("InMemory DB<br/>dev / tests")]

    SPA -- "HTTPS / JSON" --> API
    API --> INFRA
    API --> CORE
    INFRA --> CORE
    INFRA -- "production" --> DB
    INFRA -. "dev / test" .-> MEM

    TESTS["Outrally.Tests<br/>xUnit + WebApplicationFactory"] -. "calls over HTTP" .-> API
```

**Key rule:** dependencies only point **inward, toward `Core`**. `Core` doesn't know the database, the web framework or the tests exist.

---

## Solution structure

```
outrally/
├── Outrally.Core/           # Domain models and enums. No dependencies on other projects.
├── Outrally.Infrastructure/ # AppDbContext, EF Core, migrations. Depends on Core.
├── Outrally.Api/            # ASP.NET Core controllers, middleware, Program.cs. Depends on Core + Infrastructure.
├── Outrally.Tests/          # xUnit integration tests using WebApplicationFactory. Depends on Core + Api.
└── web/                     # Angular 22 SPA
```

### Why each project exists

| Project | What it holds | Why it's separate |
|---|---|---|
| **Outrally.Core** | Domain models (`Activity`) and enums (`ActivityStatus`) | This is the heart of the app: the business concepts. It depends on **nothing**, so it never breaks because of a database or framework change. |
| **Outrally.Infrastructure** | `AppDbContext`, EF Core configuration, migrations | It's the only project that knows how to talk to the database. If we ever switch databases, only this project changes. |
| **Outrally.Api** | Controllers, middleware, `Program.cs` (DI, CORS, Swagger) | A **thin** HTTP layer. It receives requests, delegates the work and returns responses. Business rules don't belong here. |
| **Outrally.Tests** | xUnit integration tests | Tests the **full HTTP pipeline** (routing, controllers, serialization, DbContext) with an in-memory database, so tests are fast and need no SQL Server. |
| **web/** | Angular SPA | The user interface. It's deployed separately from the API and talks to it only through HTTP. |

### Why layered?

- **Testable:** `Core` has no dependencies, so it's easy to test. The API can run against an in-memory database in tests.
- **Replaceable:** you can change one layer (for example, the database) without rewriting the others.
- **Easy to navigate:** once you know the rule, you know where new code goes. A new entity goes in `Core`, a new table mapping in `Infrastructure`, a new endpoint in `Api`.

> **Rule of thumb:** if you find yourself adding a `using Microsoft.EntityFrameworkCore;` inside `Outrally.Core`, stop. That code belongs in `Infrastructure`.

---

## Tech stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core 9 Web API (C#) |
| Frontend | Angular 22 SPA (TypeScript, SCSS) |
| Database | SQL Server (hosted on SmarterASP.NET) |
| ORM | Entity Framework Core 9, code-first |
| Dev/Test database | EF Core InMemory provider |
| Testing | xUnit, Moq, FluentAssertions, `WebApplicationFactory` |
| API docs | Swagger / OpenAPI |
| CI/CD | GitHub Actions |
| API hosting | Azure App Service |
| Frontend hosting | SmarterASP.NET |

---

## Running locally

### Prerequisites

Install these first:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). Check with `dotnet --version`.
- [Node.js](https://nodejs.org/), in a version supported by Angular 22. Check the [Angular version compatibility guide](https://angular.dev/reference/versions) and `node --version`.
- Angular CLI: `npm install -g @angular/cli`
- Git
- *(Optional)* An editor: Visual Studio 2022, JetBrains Rider or VS Code with the C# Dev Kit
- *(Optional)* SQL Server. You only need it to test against a real database. Local development uses the InMemory database by default.

### 1. Clone the repo

```bash
git clone <repo-url> outrally
cd outrally
```

### 2. Run the API

```bash
dotnet restore
dotnet build
dotnet run --project Outrally.Api
```

The console prints the URL the API is listening on (from `Outrally.Api/Properties/launchSettings.json`). Then:

- Open **Swagger UI** at `https://localhost:<port>/swagger` to explore and try the endpoints.
- Try the endpoint directly at `https://localhost:<port>/api/activities`. Expect `[]` on a fresh InMemory database.

#### Required secrets (Phase 2+)

Since Phase 2, the API needs JWT configuration or it crashes silently on startup. Set these once per machine, from the `Outrally.Api` folder:

```bash
cd Outrally.Api
dotnet user-secrets set "Jwt:Key" "your-random-key-minimum-32-characters"
dotnet user-secrets set "Jwt:Issuer" "outrally-api"
dotnet user-secrets set "Jwt:Audience" "outrally-web"
```

Generate a safe key with `openssl rand -base64 32`.

> User secrets are stored in your OS user profile, never in the repo. See [JWT: login crashes with `IDX10720`](#jwt-login-crashes-with-idx10720-signing-key-too-short) for why the key must be at least 32 characters.

#### InMemory vs SQL Server

`Program.cs` chooses the database provider at startup:

- **Development / tests**: EF Core **InMemory**. There's nothing to install, and data resets every time the app restarts.
- **Production**: **SQL Server**, using the connection string from configuration.

To run locally against SQL Server, set the connection string with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) so it never gets committed:

```bash
cd Outrally.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=Outrally;..."
```

If `ConnectionStrings:DefaultConnection` is empty or missing, the app uses InMemory.

> ⚠️ **Never commit connection strings or passwords.** Use user secrets locally and App Service configuration in Azure.

### 3. Run the Angular app

In a second terminal:

```bash
cd web
npm install
ng serve
```

Open `http://localhost:4200`. The API's CORS policy must allow this origin. It's configured in `Program.cs`.

### 4. First run

With both the API and the Angular app running, open `http://localhost:4200`. You'll be redirected to `/register`.

1. Create an account. New users get the **Outdoor Enthusiast (OE)** role by default.
2. Sign in at `/login`.
3. You land on the activities screen.

> With the InMemory database, users disappear when the API restarts, so you'll need to register again.

---

## Deploying to Azure

The API is hosted on **Azure App Service**. In the Azure portal, go to **App Service → Configuration → Application settings** and add these settings. A double underscore (`__`) in the name maps to a colon (`:`) in .NET configuration.

| Azure setting name | Maps to | Value |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | The SQL Server connection string |
| `Jwt__Key` | `Jwt:Key` | JWT signing key, 32+ characters |
| `Jwt__Issuer` | `Jwt:Issuer` | `outrally-api` |
| `Jwt__Audience` | `Jwt:Audience` | `outrally-web` |

> ⚠️ Never put these values in `appsettings.json` or commit them to the repo.

---

## Running tests

From the repository root:

```bash
dotnet test
```

Useful variations:

```bash
# More detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a single test by name
dotnet test --filter "FullyQualifiedName~GetActivities_ReturnsOk"
```

### How the tests work

The tests in `Outrally.Tests` are **integration tests**. They don't call controller methods directly. Instead:

1. `WebApplicationFactory<Program>` starts the real API **in memory**, with no network and no deployed server.
2. The test replaces the database with the **InMemory** provider, so each test starts from a known state.
3. The test sends a real HTTP request through an `HttpClient` and checks the response with **FluentAssertions**.

So one test exercises routing, the controller, JSON serialization and EF Core together, which is very close to what a real client does.

Frontend tests (Angular) run from `web/`:

```bash
cd web
ng test
```

---

## Phase log

Each phase is a small, working, tested slice of the app. When you finish a phase, update this section.

### ✅ Phase 1: Foundation and first endpoint (complete)

**Goal:** prove the architecture end to end, from HTTP request to database and back, with tests and CI in place from day 1.

**What was built:**

- **`Activity` model** (`Outrally.Core`)
  | Property | Purpose |
  |---|---|
  | `Id` | Primary key |
  | `Title` | Short name, e.g. "Ron Coleman Trail" |
  | `Description` | Details, gear and notes |
  | `ScheduledDate` | When it happens |
  | `Location` | Trailhead or meeting point |
  | `MaxParticipants` | Capacity limit |
  | `Status` | See `ActivityStatus` |
  | `CreatedAt` | When the record was created |
- **`ActivityStatus` enum**: `Upcoming`, `InProgress`, `Completed`, `Cancelled`
- **`AppDbContext`** (`Outrally.Infrastructure`) with an `Activities` DbSet
- **`Program.cs`** with CORS, Swagger and the InMemory/SQL Server switch
- **`GET /api/activities`**, which returns the list of activities
- **Two TDD tests**, each written first and watched fail (Red), then made to pass (Green):
  - `GetActivities_ReturnsOk`: the endpoint exists and returns HTTP 200
  - `GetActivities_ReturnsEmptyList_WhenNoActivities`: an empty database returns `[]`, not `null` and not an error
- **GitHub Actions** pipeline that builds and runs tests

**Lessons for juniors:**

- The first test failed because the endpoint didn't exist yet. **That's the point.** A test you've never seen fail might not be testing anything.
- Test the "empty" case early. Returning `null` instead of `[]` is a common bug that breaks front-ends.

### ✅ Phase 2: Auth & Users (complete)

**Goal:** let users register and log in, and give every request an identity, a role and an organization.

**What was built:**

- **ASP.NET Identity + JWT authentication** for register and login
- **`ApplicationUser`**, which extends `IdentityUser` with:
  | Property | Purpose |
  |---|---|
  | `FirstName`, `LastName` | Display name |
  | `Role` | `OE` (Outdoor Enthusiast), `TL` (Team Leader) or `AD` (Admin) |
  | `OrganizationId` | The organization (tenant) the user belongs to |
- **`Organization` entity.** There's a single organization for now, but every user has an `OrganizationId`, so the app can become multi-tenant without schema changes.
- **`AuthController`**
  | Endpoint | Purpose |
  |---|---|
  | `POST /api/auth/register` | Creates a user |
  | `POST /api/auth/login` | Returns a JWT |
- **JWT tokens** that include a **role claim** and **expire after 8 hours**
- **Default organization seeding:** one is created on startup if none exists
- **Angular auth (frontend)**
  | File | Purpose |
  |---|---|
  | `web/src/app/services/auth.service.ts` | Handles register, login and logout. Stores the JWT and current user in `localStorage`. Exposes `currentUser` as a signal. |
  | `web/src/app/interceptors/auth.interceptor.ts` | Attaches `Authorization: Bearer <token>` to every outgoing HTTP request. |
  | `web/src/app/guards/auth.guard.ts` | Protects routes that require login. Redirects to `/login` if no token is found. |
  | `web/src/app/auth/login.component.ts` | Login form at `/login`. Navigates to `/activities` on success. |
  | `web/src/app/auth/register.component.ts` | Registration form at `/register`. Navigates to `/login` on success. |
  | `web/src/app/app.routes.ts` | Defines the routes `/login`, `/register` and `/activities` (guarded). |

  Routes are lazy-loaded with `loadComponent`, so each screen's code downloads only when the user first opens it.

**Lessons for juniors:**

- **`ApplicationUser` lives in `Outrally.Infrastructure`, not `Outrally.Core`.** It inherits from `IdentityUser`, which comes from the Identity packages. `Core` must not depend on those packages, so putting the user class in `Infrastructure` keeps the domain layer clean. This is the "dependencies point inward" rule in practice.
- **Missing JWT secrets make the app crash silently.** If the JWT settings aren't configured locally, the API exits without a useful message. Wrapping `Program.cs` in a `try/catch` surfaces the real error:

  ```csharp
  try
  {
      var builder = WebApplication.CreateBuilder(args);
      // ... service registration ...
      var app = builder.Build();
      // ... middleware ...
      app.Run();
  }
  catch (Exception ex)
  {
      Console.WriteLine($"Startup failed: {ex}");
      throw;
  }
  ```

  Set the secrets with `dotnet user-secrets set`, and run it **from the `Outrally.Api` folder**, because user secrets belong to a specific project:

  ```bash
  cd Outrally.Api
  dotnet user-secrets set "Jwt:Key" "<32+ character key>"
  dotnet user-secrets set "Jwt:Issuer" "outrally-api"
  dotnet user-secrets set "Jwt:Audience" "outrally-web"
  ```

- **Integration tests need two changes to swap SQL Server for InMemory.** In `WithWebHostBuilder`, you must do **both** of these:
  1. Override the connection string to an empty string, so the app chooses InMemory:
     `UseSetting("ConnectionStrings:DefaultConnection", "")`
  2. Remove **both** the `DbContextOptions<AppDbContext>` **and** the `AppDbContext` service descriptors before registering InMemory.

  If you remove only the options descriptor, the SQL Server provider stays registered alongside InMemory, and the test host crashes at startup with a "multiple database providers" error.

  ```csharp
  factory.WithWebHostBuilder(builder =>
  {
      builder.UseSetting("ConnectionStrings:DefaultConnection", "");

      builder.ConfigureServices(services =>
      {
          var toRemove = services
              .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                       || d.ServiceType == typeof(AppDbContext))
              .ToList();
          foreach (var d in toRemove) services.Remove(d);

          services.AddDbContext<AppDbContext>(o =>
              o.UseInMemoryDatabase("TestDb"));
      });
  });
  ```

### ⬜ Phase 3: _TBD_

> Placeholder.

### ⬜ Phase 4: _TBD_

> Placeholder.

---

## Troubleshooting notes

Short notes on things that tripped us up. If you lose more than 15 minutes to a problem, add it here.

### Angular 22: root component files are `app.ts` / `app.html`, not `app.component.*`

**Symptom:** tutorials, Stack Overflow answers and older docs tell you to edit `app.component.ts` and `app.component.html`, but those files don't exist in `web/src/app/`.

**Cause:** Angular 22 changed the default naming convention for the root component.

| Older Angular | Angular 22 |
|---|---|
| `app.component.ts` | `app.ts` |
| `app.component.html` | `app.html` |
| class `AppComponent` | class `App` |

**Fix:** to add a new component to the root, edit `app.ts` (add it to the `imports` array) and `app.html` (use its selector). The bootstrapped class is `App`, so anything that references the root component by name should use `App`, not `AppComponent`.

### Angular 22: the UI doesn't update after an HTTP call (zoneless by default)

**Symptom:** an HTTP request succeeds and the component's variable gets the new value (you can see it in the debugger or a `console.log`), but the screen never changes. For example, a loading spinner stays on forever, or a list stays empty.

**Cause:** Angular 22 is **zoneless by default**. Earlier Angular versions included `zone.js`, a library that automatically detected async operations (HTTP calls, timers, events) and told Angular to re-render. Angular 22 doesn't include it. Without zone.js, a plain class field can change in memory without Angular ever redrawing the screen.

**Fix:** use **Angular Signals** (`signal<T>()`) for any state that changes after an async operation. Angular tracks where a signal is read and re-renders automatically when its value changes.

| Before (doesn't re-render) | After (with signals) |
|---|---|
| `loading = true;` | `loading = signal(true);` |
| `this.loading = false;` | `this.loading.set(false);` |
| `*ngIf="loading"` | `*ngIf="loading()"` |

```typescript
import { Component, inject, signal } from '@angular/core';

export class ActivityList {
  private http = inject(HttpClient);

  loading = signal(true);
  activities = signal<Activity[]>([]);

  ngOnInit() {
    this.http.get<Activity[]>('/api/activities').subscribe(data => {
      this.activities.set(data);  // triggers a re-render
      this.loading.set(false);    // triggers a re-render
    });
  }
}
```

```html
<p *ngIf="loading()">Loading…</p>
<li *ngFor="let a of activities()">{{ a.title }}</li>
```

**Remember:**

- In templates, **call signals as functions**: `loading()`, not `loading`. Forgetting the `()` is the most common mistake. You get the signal object instead of its value.
- Apply this to **all** component state that depends on HTTP responses or other async work, not only loading flags.

### JWT: login crashes with `IDX10720` (signing key too short)

**Symptom:** the API starts normally, but `POST /api/auth/login` fails with:

```
IDX10720: Unable to create KeyedHashAlgorithm
```

**Cause:** the HS256 algorithm requires a signing key of **at least 256 bits (32 characters)**. The key isn't used until the API signs a token, so a short key in user secrets doesn't fail at startup. It fails the first time someone logs in.

**Fix:** use a JWT signing key of **at least 32 characters**. A random one is safest:

```bash
# Generates a random 44-character key
openssl rand -base64 32

cd Outrally.Api
dotnet user-secrets set "Jwt:Key" "<generated key>"
```

> **Takeaway:** "it started fine" doesn't mean the configuration is valid. Some settings are only checked when the code that uses them runs, so test the login flow after changing secrets.

---

## Contributing and development approach

### 1. Test-driven development (TDD)

Every feature follows **Red → Green → Refactor**:

1. **Red:** write a test for the behavior you want. Run it and **watch it fail**.
2. **Green:** write the **minimum** code to make it pass. Don't add extras.
3. **Refactor:** clean up names, remove duplication and simplify, keeping all tests green.

Name tests as `Method_ExpectedResult_WhenCondition`, for example `GetActivities_ReturnsEmptyList_WhenNoActivities`. Someone should be able to read the test name and know what broke.

### 2. CI/CD from day 1

- Every push and pull request runs the **GitHub Actions** pipeline: restore, build and test.
- **A red pipeline is everyone's top priority.** Don't merge on red.
- Run `dotnet test` locally before you push. It's faster than waiting for CI to tell you.

### 3. Respect the layers

| If you're adding… | It goes in… |
|---|---|
| A new entity or enum | `Outrally.Core` |
| A table mapping, migration or query | `Outrally.Infrastructure` |
| A new endpoint or middleware | `Outrally.Api` |
| A test | `Outrally.Tests` |
| UI | `web/` |

Dependencies flow **inward toward `Core`**, never outward.

### 4. Database changes (EF Core code-first)

Change the model in `Core`, then create a migration:

```bash
dotnet ef migrations add <DescriptiveName> \
  --project Outrally.Infrastructure \
  --startup-project Outrally.Api
```

Use descriptive names like `AddParticipantsTable`, not `Update1`. If `dotnet ef` isn't found, install it with `dotnet tool install --global dotnet-ef`.

### 5. Workflow

1. Create a branch from `main`: `feature/<short-description>` or `fix/<short-description>`.
2. Do Red → Green → Refactor in small commits.
3. Make sure `dotnet test` passes locally.
4. Open a pull request with a clear description of **what** changed and **why**.
5. **Update this README.** If you finished a phase or changed how to run something, document it. The next junior developer depends on it.

### 6. Secrets

- Don't commit connection strings, API keys or passwords.
- Use `dotnet user-secrets` locally and **Azure App Service configuration** or **GitHub Actions secrets** for deployed environments.
