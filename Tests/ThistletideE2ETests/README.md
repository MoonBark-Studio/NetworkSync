# Thistletide E2E Network Sync Tests

> ⚠️ **Player Cap Recommendation**: Thistletide should cap max concurrent players at **64** based on LiteNetLib benchmarks.

This test project provides end-to-end stress testing of the NetworkSync plugin using Thistletide's occupancy map API.

## Overview

The tests use a mock implementation of Thistletide's `TrackedPlacementOccupancyMap` to test:
- 64 concurrent connections
- 128 concurrent connections  
- Data sync verification between server and clients
- Performance metrics (latency, throughput)

## Project Structure

- `ThistletideMocks.cs` - Mock implementations of Thistletide types:
  - `TrackedPlacementOccupancyMap` - Grid-based occupancy tracking
  - `PlacementCellOccupancy` - Cell data structure
  - `CoreVector2I` - 2D integer vector

- `ThistletideTestServer.cs` - Server implementing `ICoreOccupancyProvider`
- `ThistletideTestClient.cs` - Client implementing `ILocalOccupancyValidator`
- `ThistletideStressTestRunner.cs` - Test orchestration and metrics
- `Program.cs` - Entry point

## Running the Tests

For test execution commands and coverage generation, see [../../TEST_COVERAGE.md](../../TEST_COVERAGE.md).

## Test Output

The tests output detailed metrics including:

- **Connection Metrics**: Total, successful, failed connections; connection times
- **Message Metrics**: Messages sent/received, latency (min/max/avg)
- **Placement Metrics**: Total commands, success/fail counts, throughput
- **Sync Quality**: Server placements, sync mismatches

## Performance Limits

For detailed performance metrics, connection limits, and stress test results, see [../StressTests/NETWORK_SYNC_LIMITS.md](../StressTests/NETWORK_SYNC_LIMITS.md).

### Quick Reference

| Max Players | Latency | Status |
|-------------|---------|--------|
| **64** | <20ms | ✅ Recommended |
| 128 | <30ms | ⚠️ Use with caution |

## Integration with Real Thistletide

To use with the actual Thistletide.Core assembly:

1. Add reference to `Thistletide.Core.dll`
2. Replace mock `TrackedPlacementOccupancyMap` with real implementation
3. Use `NetworkSyncOccupancyProviderAdapter` and `NetworkSyncLocalOccupancyValidatorAdapter`

## Architecture

The tests verify the complete data flow:
1. Client sends `PlacementCommandMessage`
2. Server validates via `ICoreOccupancyProvider.IsPlacementValidAsync`
3. Server applies placement via `ICoreOccupancyProvider.ApplyPlacementAsync`
4. Server broadcasts `PlacementDeltaMessage` and `OccupancyDeltaMessage`
5. Clients receive deltas and update local `ILocalOccupancyValidator`
6. Test verifies client state matches server state
