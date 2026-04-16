# NetworkSync — Roadmap
Updated: 2026-04-14

## Overview
Authoritative state synchronization for multiplayer games.

## Development Phases

### Phase 0: Framework Alignment (P0)
- [ ] Define `ISyncProvider` in `MoonBark.Framework.Network`.
- [ ] Implement `NetworkTransformComponent` using Framework primitives.
- [ ] Standardize RPC event bus on Framework events.

### Phase 1: Snapshot Sync (Current)
- [ ] Delta-compressed entity snapshots.
- [ ] Client-side prediction for local player.

### Phase 2: Reconciliation
- [ ] Server-authoritative rollback and reconciliation.
- [ ] Latency compensation (lag compensation).

### Phase 3: Scalability
- [ ] Network interest management (relevance zones).
- [ ] Compression for high-bandwidth data (physics sync).
