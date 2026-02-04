# Database Migrations

This project uses Entity Framework Core with PostgreSQL (Npgsql) and a single `AppDbContext` containing all entity tables.

## Prerequisites

Install the EF Core CLI tool (one-time):

```bash
dotnet tool install --global dotnet-ef
```

All migration commands below are run from the **solution root** (`SportsBettingPipeline/`).

## Creating Migrations

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure \
  --startup-project src/Sandbox
```

Replace `<MigrationName>` with a descriptive name (e.g., `InitialCreate`, `AddTeamIndex`, `AddEventColumn`).

Migrations are generated in `src/Infrastructure/Migrations/`.

## Applying Migrations

### Apply to a running database

```bash
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/Sandbox
```

The connection string is read from `src/Sandbox/appsettings.json`. Make sure PostgreSQL is running and the connection string is correct before applying.

### Apply at runtime (optional)

You can also apply pending migrations at application startup:

```csharp
await dbContext.Database.MigrateAsync();
```

This is useful for Lambda cold starts but should be used cautiously in production.

## Generating SQL Scripts

To review what a migration will do without applying it:

```bash
dotnet ef migrations script \
  --project src/Infrastructure \
  --startup-project src/Sandbox \
  --output migrations.sql
```

## Removing the Last Migration

If a migration hasn't been applied yet and you want to redo it:

```bash
dotnet ef migrations remove \
  --project src/Infrastructure \
  --startup-project src/Sandbox
```

## Listing Migrations

```bash
dotnet ef migrations list \
  --project src/Infrastructure \
  --startup-project src/Sandbox
```

## Common Workflow

1. Modify an entity in `src/Core/Models/Entities/` or a configuration in `src/Infrastructure/Data/Configurations/`
2. Create a migration (see command above)
3. Review the generated migration file in `src/Infrastructure/Migrations/`
4. Apply the migration to your local database
5. Commit the migration files to source control

## Connection String

Configured in `appsettings.json` under the `Database` section:

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=sportsbetting;Username=postgres;Password=postgres"
  }
}
```

For Lambda, set via environment variable: `Database__ConnectionString`.
