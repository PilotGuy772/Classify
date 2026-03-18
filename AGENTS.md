# AGENTS.md — Guide for AI coding agents

Purpose: Give an AI agent the minimum concrete knowledge to be productive in this repo: architecture, key files, dev workflows, project-specific conventions, and concrete examples/commands.

1) Quick start (how to build/run/test)
- Build solution: `dotnet build Classify.sln`
- Run desktop app (Avalonia UI): `dotnet run --project Classify.Desktop`
- Run tests: `dotnet test Tests`
- Add / run EF migrations (use Desktop as startup project):
  - Add migration: `dotnet ef migrations add <Name> --project Classify.Data --startup-project Classify.Desktop --context ClassifyContext`
  - Update DB: `dotnet ef database update --project Classify.Data --startup-project Classify.Desktop --context ClassifyContext`

2) High-level architecture
- Multi-project .NET solution (`Classify.sln`) with clear boundaries:
  - `Classify.Core` — domain models and interfaces (entities like `AudioFile`, `Recording`, `Work`). See `Classify.Core/Domain`.
  - `Classify.Data` — EF Core DbContext, configurations, repositories, migrations and seeders. Key files: `Classify.Data/Context/ClassifyContext.cs`, `Classify.Data/UnitOfWork.cs`, `Classify.Data/Configurations/*`, `Classify.Data/Migrations/`.
  - `Classify.Services` — business/use-case services (ingestion pipeline). See `Classify.Services/Ingestion`.
  - `Classify.Infrastructure` — infra helpers (logging, dialog services, etc.).
  - `Classify.Desktop` — Avalonia UI, DI registration and app bootstrap (`App.axaml.cs`, `Program.cs`, `appsettings.json`).
  - `Tests` — unit/integration tests and test helpers (`Tests/SqliteInMemory.cs`).

3) Core patterns & conventions (project-specific)
- DI-first design: services and repositories are registered in `Classify.Desktop/App.axaml.cs` using `Microsoft.Extensions.DependencyInjection`.
  - Repositories are registered as scoped, `IUnitOfWork` is registered transient, ViewModels are registered as singletons/transients per file.
- EF Core usage
  - `ClassifyContext` exposes DbSets and applies configurations from assembly (`ApplyConfigurationsFromAssembly`). Default ctor uses SQLite `library.db` if not constructed via options (useful for lightweight runs).
  - Project uses `AddDbContextFactory<ClassifyContext>` (factory pattern). See `App.axaml.cs` and `Tests/SqliteInMemory.cs` which mirror the same pattern.
  - Migrations live in `Classify.Data/Migrations/` and runtime migration is triggered by `context.Database.MigrateAsync()` in startup seed flow.
- UnitOfWork pattern
  - `Classify.Data/UnitOfWork.cs` creates a single DbContext from `IDbContextFactory<ClassifyContext>` and exposes lazy-initialized repositories (see `field ??= new ...` pattern used across properties).
- MVVM UI
  - Desktop uses Avalonia with MVVM. ViewModels are registered in DI and `MainWindowViewModel` is created and assigned as DataContext in `App.axaml.cs`.

4) Important integration points / external dependencies
- SQLite via EF Core: `Microsoft.EntityFrameworkCore.Sqlite` (see `Classify.Data.csproj`).
- Avalonia UI packages in `Classify.Desktop.csproj` (Avalonia, themes, fonts).
- `Bogus` is used in seeders to generate demo data (`DemoLibrarySeeder.cs`).
- `appsettings.json` in `Classify.Desktop` contains `AppSettings.LibraryPath` (points at `DevLibrary/`) used by ingestion services.

5) Concrete code examples to reference
- DbContext default and ApplyConfigurations: `Classify.Data/Context/ClassifyContext.cs` — default ctor uses `UseSqlite("Data Source=library.db")` and `ApplyConfigurationsFromAssembly(...)`.
- UnitOfWork shared DbContext and lazy repos: `Classify.Data/UnitOfWork.cs` — constructs context with `contextFactory.CreateDbContext()` and exposes repositories via `field ??= ...`.
- Startup seed flow: `Classify.Desktop/App.axaml.cs` — builds DI, obtains `IUnitOfWork`, calls `context.Database.MigrateAsync()` and runs an `IDatabaseSeeder` (DemoLibrarySeeder in DEBUG builds).
- Tests use in-memory SQLite: `Tests/SqliteInMemory.cs` (DataSource=:memory:) and call `db.Database.Migrate()` to apply migrations in test runs.

6) Developer tips & troubleshooting (repo-specific)
- EF CLI: use the `--startup-project Classify.Desktop` flag so migrations resolve the same DI/startup code (DbContextFactory is registered in Desktop). If you get `No DbContext was found`, confirm `Microsoft.EntityFrameworkCore.Design` is present in the startup project's package refs (it is included in `Classify.Desktop.csproj`).
- Running the desktop app will auto-seed the DB in DEBUG via `DemoLibrarySeeder`. To reset local DB remove `library.db` in the working dir.
- Tests rely on the migration files; if tests fail with missing tables, ensure `dotnet ef` migrations have been applied or that tests call `context.Database.Migrate()` (they do).
- .NET Target: projects target `net10.0`. Ensure local SDK matches or is newer.

7) Where to look next (files to open for tasks)
- Implement data queries: `Classify.Data/Repositories/*` (repository base is `Repository<T>` and concrete repos like `AudioFileRepository`).
- Add ingestion logic: `Classify.Services/Ingestion/*` and `Classify.Desktop/App.axaml.cs` registration.
- UI work: `Classify.Desktop/ViewModels/*` and `Classify.Desktop/Views/*`.

If you need a focused agent task (implement a new repository method, add a migration, or create a ViewModel), point to the exact files above and include a small acceptance test (e.g., `dotnet test` scenario or a manual run command).

