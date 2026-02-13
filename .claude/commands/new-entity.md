Create a new backend entity with EF Core configuration and migration.

Ask the user for:
1. **Entity name** (PascalCase, e.g., `Order`)
2. **Properties** (name, type, nullability)
3. **Feature name** (usually same as entity, but may differ)
4. **Any enum properties** (if yes, ask for enum members with explicit integer values)

## Execution

Follow **SKILLS.md → "Add an Entity (End-to-End)"**, but stop after Infrastructure (skip Application/WebApi — those are for `/new-endpoint`).

Scope: Domain entity → error messages → EF config → DbSet → migration.

Read `src/backend/AGENTS.md` for conventions on entity design, EF configuration, and the Options pattern if needed.

After the migration, verify with `dotnet build src/backend/MyProject.slnx`.

Commit: `feat({feature}): add {Entity} entity and EF configuration`

Check **FILEMAP.md** impact tables if modifying an existing entity instead of creating a new one.
