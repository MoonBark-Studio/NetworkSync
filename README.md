# NetworkSync

<!--
SSOT template for plugin README.md files.
Copy this file to the plugin root as README.md and fill in the sections.
Cross-game architecture rules: see docs/plugin-game-boundaries.md
DRY violations tracked in: docs/integrations/dry-refactor-candidates.md
-->

## Role

One-paragraph description of what this plugin owns and what it does NOT own.
See plugin-game-boundaries.md for the full boundary rules.

## What belongs in this plugin

- Bullet list of key components and systems

## What stays game-specific

- Bullet list of what games must implement themselves

## Key Types

List the primary public types with a brief description.

## Dependencies

- Friflo.Engine.ECS version (if used)
- MoonBark.Framework (list the key interfaces/classes consumed)

## Usage

```csharp
// Minimal example showing the key API
```

## Architecture

```
NetworkSync/
├── Core/
│   ├── Abstractions/      # Interfaces (game-facing contracts)
│   ├── Components/        # ECS components (if applicable)
│   ├── Systems/          # ECS systems (if applicable)
│   └── ...
├── ECS/                   # ECS integration (IEcsPlugin, etc.)
├── Godot/                 # Godot layer (if applicable)
├── Tests/                 # Test project
└── docs/
    └── integrating.md      # Game integration guide (if applicable)
```

## Validation

```bash
# Build
dotnet build NetworkSync.Core.csproj

# Test
dotnet test Tests/NetworkSync.Tests.csproj
```

## Related Documentation

- [Plugin vs Game Boundaries](../../docs/plugin-game-boundaries.md) — architecture rules
- [DRY Refactor Candidates](../../docs/integrations/dry-refactor-candidates.md) — cross-game duplication tracking

## Version

0.1.0 — Not production ready