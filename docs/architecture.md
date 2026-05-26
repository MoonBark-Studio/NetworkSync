# MoonBark NetworkSync Architecture Blueprint

## Architectural Layers

```
  ┌─────────────────────────────────────────┐
  │                 Godot                   │ (Integration & Nodes)
  └────────────────────┬────────────────────┘
                       ▼
  ┌─────────────────────────────────────────┐
  │                  ECS                    │ (Systems & Components)
  └────────────────────┬────────────────────┘
                       ▼
  ┌─────────────────────────────────────────┐
  │                  Core                   │ (Pure C# Domain Logic)
  └─────────────────────────────────────────┘
```

### 1. Core Layer
Pure domain models and interfaces containing no external or game engine dependencies.

### 2. ECS Layer
Friflo ECS Components and Systems containing event listeners and simulation logic.

### 3. Godot Layer
Godot Engine nodes and editor integrations wrapping the ECS layer.
