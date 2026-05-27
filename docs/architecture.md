# MoonBark NetworkSync Architecture Blueprint

## Architectural Layers

```
  ┌─────────────────────────────────────────┐
  │                 Godot                   │ (Nodes, HUD, Input)
  └────────────────────┬────────────────────┘
                       ▼
  ┌─────────────────────────────────────────┐
  │               Game Core                 │ (Headless ECS, Policies, Tasks)
  │  ┌───────────────────────────────────┐  │
  │  │  ECS (implementation detail)     │  │
  │  └───────────────────────────────────┘  │
  └────────────────────┬────────────────────┘
                       ▼
  ┌─────────────────────────────────────────┐
  │              Framework                  │ (Shared types, buses, ECS base)
  └─────────────────────────────────────────┘
```

### 1. Core Layer
Pure domain models, interfaces, and ECS components — no Godot dependencies.

### 2. ECS Integration
Friflo ECS systems live within Core as an implementation detail. See docs/plugin-game-boundaries.md.

### 3. Godot Layer
Godot Engine nodes and editor integrations — only for scene wiring, HUD, and input. No domain logic here.
