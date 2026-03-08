# NetworkSync Test Coverage

## Current Automated Coverage

The current fast automated slice lives in:

- `Tests/StressTests/AuthorityAndPredictionTests.cs`
- `Tests/StressTests/ReplicationServiceTests.cs`
- `Tests/StressTests/StressInfrastructureTests.cs`

### E2E Tests

For end-to-end stress testing with Thistletide mocks, see [Tests/ThistletideE2ETests/README.md](Tests/ThistletideE2ETests/README.md).

For performance limits documentation, see [Tests/ThistletideE2ETests/NETWORK_SYNC_LIMITS.md](Tests/ThistletideE2ETests/NETWORK_SYNC_LIMITS.md).

## Covered Areas

### Core services

- `ReplicationService`
  - placement delta publish
  - occupancy delta publish
  - placement delta processing
  - snapshot request dispatch
  - changed-cell tracking and clearing

- `ServerAuthorityService`
  - valid placement acceptance
  - invalid placement rejection
  - command result caching

- `ClientPredictionService`
  - valid local prediction
  - mismatch reconciliation
  - mismatch event emission
  - pending command tracking

### Stress test support infrastructure

- `TestOccupancyProvider`
  - bounds validation
  - duplicate rejection
  - placement persistence

- `TestLocalValidator`
  - occupancy transitions
  - local placement count tracking

- `TestMetrics`
  - connection aggregation
  - message aggregation
  - placement aggregation
  - sync mismatch tracking
  - snapshot generation

## Test Results

Latest successful local run:

- **Test project**: `Tests/StressTests/NetworkSync.StressTests.csproj`
- **Result**: `12/12` tests passing
- **Coverage artifact**:
  - `Tests/StressTests/TestResults/ba29c0d1-1454-420c-ada8-43058f27c774/coverage.cobertura.xml`

## Commands

Run tests:

```powershell
dotnet test c:\dev\godot\projects\plugins\NetworkSync\Tests\StressTests\NetworkSync.StressTests.csproj
```

Run tests with coverage:

```powershell
dotnet test c:\dev\godot\projects\plugins\NetworkSync\Tests\StressTests\NetworkSync.StressTests.csproj --collect:"XPlat Code Coverage"
```

## Current Gaps

The following areas still need deeper coverage:

- `LiteNetTransport` real socket behavior against live peers
- `NetworkManager` end-to-end server/client orchestration with real transport
- high-concurrency stress scenarios as assertions instead of console-driven scripts
- snapshot reconciliation and chunk delta flows
- disconnect/reconnect resilience

## Recommended Next Additions

1. Add loopback integration tests for `LiteNetTransport`
2. Add `NetworkManager` integration tests with a real server/client pair
3. Convert the existing heavier stress scripts into `[Fact]` or `[Theory]` cases with bounded timeouts
4. Add coverage parsing/report automation once the plugin joins a larger solution
