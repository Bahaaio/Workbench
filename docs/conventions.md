# Workbench — Conventions

> Read this before writing code. It exists so agents and humans make changes
> that look like they were made by the same person.
>
> Stack: .NET 10 · ASP.NET Core · Blazor Server (InteractiveServer) · EF Core +
> Npgsql · MudBlazor v9 · xUnit/Moq. Single project (`Workbench/`) + tests
> (`Workbench.Tests/`). Product name "Workbench", code namespace `Workbench`.

## Solution layout (vertical slices)

```
Workbench/
├── Modules/<Slice>/          # one folder per feature — everything it owns lives here
├── Common/                   # shared kernel — contracts & helpers only, NO behavior owned here
│   └── Exceptions|Options|Models|Extensions
├── Data/
│   ├── Migrations/           # EF Core migrations
│   └── AppDbContext.cs       # single DbContext (central, never per-module)
├── ClientServices/           # Blazor-side clients (Iface + Implementations/)
├── Components/               # Blazor shell: App, Routes, Layout/, Pages/<Feature>/
└── Extensions/               # Program.cs bootstrap glue ONLY (OpenApi, Ui, Security…)
```

**Folder placement rule of thumb:** ask _"who consumes this?"_

- Only slice X → inside `Modules/X`
- Several slices → `Common/` (contracts/helpers) or its own infra slice
- Only Program.cs → root `Extensions/`

## Module internal structure

**Every module uses the same layered structure — always**, regardless of size
(even a 2-file module follows the pattern; folders only exist for kinds the
module actually has):

```
Modules/<Slice>/
├── Models/               # entities + domain enums
├── Enums/                # (or inside Models/ — pick per slice, stay consistent)
├── Dtos/                 # response records; Requests/ subfolder for inputs
├── Options/              # module-owned configuration (IKeyableOptions)
├── Configuration/        # EF Core IEntityTypeConfiguration<T>
├── Services/             # IXxxService interfaces
│   └── Implementations/  # XxxService classes
├── Mappers/              # entity→DTO mapping
├── Extensions/           # slice-only query/helper extensions
├── Controllers/
├── <SubFeature>/         # tightly-coupled child features nest here (e.g. Issues/Votes)
└── DependencyInjection.cs
```

Child features that cannot exist without their aggregate **nest inside it**
(`Issues/Votes/`) and follow the same template. Independent catalogs stay
separate (Tags manages its own CRUD → own slice). No flat modules, no
exceptions for small slices.

## Naming

Microsoft C# conventions (PascalCase types/members, camelCase locals,
`_camelCase` private fields, `I` prefix on interfaces).

| Kind          | Pattern                                                                    | Example                                          |
| ------------- | -------------------------------------------------------------------------- | ------------------------------------------------ |
| Entity        | singular noun                                                              | `Issue`, `Tag`, `Vote`                           |
| Response DTO  | `XxxDto`                                                                   | `IssueDto`, `AttachmentDto`                      |
| Request DTO   | verb/noun + `Request`                                                      | `CreateIssueRequest`, `VoteRequest`              |
| Query/options | `XxxQuery`, `XxxOptions`                                                   | `IssueQuery`, `IssueAttachmentOptions`           |
| Service       | `IXxxService` / `XxxService`                                               | `IIssuesService` / `IssuesService`               |
| EF config     | `XxxConfiguration`                                                         | `IssueConfiguration`                             |
| Mapper        | `XxxMapper`                                                                | `CommentMapper`                                  |
| Razor page    | `<Plural>` / `<Singular>Details` / `New<Singular>` / `<Qualifier><Plural>` | `Issues`, `IssueDetails`, `NewIssue`, `MyIssues` |
| Dialog        | `<Purpose>Dialog`                                                          | `CommentEditDialog`                              |
| Test          | `Method_ExpectedBehavior_WhenCondition`                                    | `GetAll_ReturnsOrderedComments_WhenTicketExists` |

No abbreviations in slice names (`Authentication`, not `Auth`).

## Services & controllers

- One `IXxxService` + `XxxService` per aggregate; services return **DTOs only** —
  entities never cross the service boundary.
- All methods `async`; controllers inject exactly one service interface and do
  nothing but translate HTTP ⇄ service calls (`Ok`, `NoContent`,
  `CreatedAtAction`).
- Interfaces live in `Services/`, implementations in `Services/Implementations/`.
- Cross-cutting helpers used by many slices go to `Common/Extensions/`.

### Error handling

- Never try/catch domain errors in controllers or services for control flow.
- Throw typed exceptions from `Common/Exceptions`: `NotFoundException`,
  `ForbiddenException`, `BadRequestException`, `ConflictException`…
- `GlobalExceptionHandler` converts them to ProblemDetails.
- Missing rows: `await _db.Issues.FindOrThrowAsync(id)` (extension on DbSet).

## Authorization

- Mechanism lives in one place: `Modules/Authorization` — guard,
  requirement(s), handler(s), `IOwnedByUser`.
- Resources implement `IOwnedByUser { int OwnerId }` (**OwnerId = owning USER id**;
  attachment parent references are named `ParentId` — never conflate them).
- Services authorize through `IAuthorizationGuard` (+ extensions like
  `AuthorizeOwnerOrTechnician`); controllers never check permissions.
- Global `FallbackPolicy = RequireAuthenticatedUser()`; public endpoints opt out
  with `[AllowAnonymous]`. Roles come from Identity (`Role.Employee/Technician`).

## Registration (DI)

- Each slice owns `<Slice>/DependencyInjection.cs`:

  ```csharp
  namespace Workbench.Modules.Comments;

  public static class DependencyInjection
  {
      extension(IServiceCollection services)
      {
          public void AddCommentsModule() { /* services + keyed options */ }
      }
  }
  ```

- `Program.cs` calls one aggregator: `builder.Services.AddModules();`
  (defined in `Extensions/ServiceExtensions.cs`).
- Options implement `IKeyableOptions` and self-register via the shared
  `services.AddKeyableOptions<T>()` helper — called inside the owning module's
  DI method, bound from `appsettings.json` by `T.Key`, validated on startup.
- `AddHttpContextAccessor()` lives in `AddAuthModule()` (its only consumer is
  `CurrentUser`).
- Seeding is a **post-build step**, never inside registration:
  `Modules/Auth/AuthSeeder.InitializeAsync()` ← `app.SeedDataAsync()`.

## Blazor UI

- Pages group by feature: `Components/Pages/<Feature>/<Page>.razor`.
  Page names follow naming convention patterns; routes are independent of location.
- Shared usings live in `Components/_Imports.razor`; add cross-feature page
  usings there too (e.g. `@using Workbench.Components.Pages.Comments` when a page
  opens another slice's dialog).
- **Never inject `IHttpContextAccessor` in components** — read identity through
  `IAuthState` (scoped, populated from `ICurrentUser`).
- Interactive rendering is global (`<Routes @rendermode="InteractiveServer" />`);
  auth login/register are static form POSTs to `/api/auth/*`.

## Attachments

- Single `Attachments` table, **TPH inheritance**: abstract `Attachment` base +
  `IssueAttachment` / `CommentAttachment` derived types.
- Derived types declare their own `ParentId : IHasParent<TParent>` mapped to
  `"IssueId"` / `"CommentId"` columns. `ParentId` ≠ `IOwnedByUser.OwnerId`.
- Generic stack in `Modules/Attachments`: `AttachmentsService<TParent,TAttachment>`
  uses an abstract `CreateAttachment(...)` factory (required members forbid
  `new()` constraints). Derived slices register closed generics:
  `IAttachmentsService<Issue>` etc.
- Limits per type from `Attachments:*` config — never hardcode sizes/extensions
  in the UI; pickers bind to `IOptions<XxxAttachmentOptions>.Value`.

## Data & migrations

- Central `Data/AppDbContext` + `Data/Migrations` — never per-module contexts.
- Configurations auto-discovered via `ApplyConfigurationsFromAssembly` — moving
  config files between folders is safe; renaming entity NAMESPACES is not
  (the migration snapshot tracks full CLR names → spurious destructive diffs).
- Connection string key: `"Default"` (Npgsql, docker compose in `Workbench/compose.yml`).

## Testing

xUnit + Moq + EF Core SQLite in-memory. Tests mirror slice namespaces.

- Services receive a real `AppDbContext` backed by SQLite in-memory (via
  `TestDbContextFactory.Create()`) and mocked `IAuthorizationGuard`,
  `ICurrentUser`, and other interface dependencies.
- **No repository mocking** — services query the real in-memory DB.
- Seed test data directly into the context; assert on DB state after operations.
- Every bug fix earns a regression test when practical.

## Git & docs

- Conventional Commits: `feat(modules): …`, `fix(ui): …`, `refactor(data): …`,
  `docs: …`. Body lists concrete changes; `BREAKING CHANGE:` footer when routes
  or schemas change.
- Verify before every commit: clean build · full test suite green · smoke-test
  touched flows (login → issues → comments → attachments).
- Canonical docs: `docs/`
  Keep them updated in the same phase as the change — stale
  docs get deleted, not ignored.
