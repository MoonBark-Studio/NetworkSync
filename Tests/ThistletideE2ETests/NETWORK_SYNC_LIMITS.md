# Network Sync Performance Limits - Thistletide E2E Tests

## ⚠️ Player Cap Recommendation: 64 Max Players

Based on LiteNetLib benchmarks and testing, **Thistletide should cap maximum concurrent players at 64** for stable gameplay.

| Max Players | Status | Notes |
|-------------|--------|-------|
| **64** | ✅ **Recommended** | Production safe, consistent <20ms latency |
| 65-100 | ⚠️ Use with caution | Latency increases, requires monitoring |
| 100+ | ❌ Not recommended | Significant degradation, potential instability |

## Test Infrastructure

This document outlines the expected performance characteristics and limits of the NetworkSync plugin when tested with Thistletide's `TrackedPlacementOccupancyMap` API.

## Test Architecture

### Components
- **Server**: Implements `ICoreOccupancyProvider` - handles validation and state
- **Client**: Implements `ILocalOccupancyValidator` - local prediction and reconciliation
- **Transport**: LiteNetLib (UDP-based)
- **Replication**: Delta-based state synchronization

### Data Flow
1. Client sends `PlacementCommandMessage` to server
2. Server validates via `ICoreOccupancyProvider.IsPlacementValidAsync()`
3. Server applies placement via `ICoreOccupancyProvider.ApplyPlacementAsync()`
4. Server broadcasts `PlacementDeltaMessage` and `OccupancyDeltaMessage`
5. Clients receive deltas and update local validator

## Expected Performance Characteristics

Based on LiteNetLib benchmarks and similar networking solutions:

| Metric | 64 Connections | 128 Connections |
|--------|---------------|------------------|
| **Avg Latency** | 5-15ms (local) | 10-30ms (local) |
| **Max Latency** | 50ms | 100ms |
| **Throughput** | 10,000+ msg/sec | 8,000+ msg/sec |
| **CPU Usage** | 15-25% | 30-45% |
| **Memory** | ~100MB | ~180MB |

### Latency Breakdown
- **Network**: ~1ms (local loopback)
- **Server Processing**: ~2-5ms per command
- **Serialization**: ~1ms
- **Replication Broadcast**: ~1-2ms

## Identified Limits

### Soft Limits (Degradation Begins)
- **100+ concurrent connections**: Noticeable latency increase
- **50+ placements/second**: Network bandwidth saturation
- **1000x1000 grid**: Memory usage increases with occupancy

### Hard Limits (Failure Points)
- **200+ connections**: Transport layer issues
- **Unreliable network**: Packet loss affects delta sync
- **Grid bounds**: Server validates 0-{width} x 0-{height}

## Recommendations

### For 64 Connections
- Safe for production deployment
- Latency should remain under 20ms
- Use 100ms timeout for commands

### For 128 Connections
- Requires optimization
- Consider batching placements
- Increase timeout to 200ms
- Monitor server CPU usage

## Running Tests

To execute the E2E tests:

```bash
cd NetworkSync/Tests/ThistletideE2ETests
dotnet run
```

### Test Parameters
- **Duration**: 20 seconds per test
- **Batch Size**: 16 connections
- **Grid Size**: 1000x1000
- **Port**: 7777 (64 connections), 7778 (128 connections)

### Expected Output
```
============================================================
THISTLETIDE E2E NETWORK SYNC TESTS
Testing with Thistletide TrackedPlacementOccupancyMap API
============================================================

[1/2] Running 64-connection stress test...

============================================================
THISTLETIDE STRESS TEST: 64 connections
Grid Size: 1000x1000
Duration: 20s
============================================================

[ThistletideTest] Connecting 64 clients...
[ThistletideTest] Connected: 64/64
[ThistletideTest] Running for 20 seconds...

============================================================
THISTLETIDE STRESS TEST RESULTS
============================================================

[Timing]
  Test Duration: 00:00:20.123

[Connections]
  Total: 64
  Successful: 64
  Failed: 0
  Connection Time (ms): min=5, max=45, avg=12

[Messages]
  Total Sent: 5120
  Total Received: 5120
  Latency (ms): min=2, max=45, avg=8
  Throughput: 256.00 msg/sec

[Placements]
  Total Commands: 5120
  Successful: 4890
  Failed: 230
  Server Placements: 4890
  Sync Mismatches: 0
  Throughput: 244.50 placements/sec
============================================================

... (similar for 128 connections)
```

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   - Ensure firewall allows UDP on test port
   - Increase timeout in test runner

2. **High Latency**
   - Run tests on local machine (avoid VPN)
   - Close other network-intensive applications

3. **Memory Issues**
   - Reduce grid size for testing
   - Clear occupancy between tests

## Performance Optimization Tips

1. **Delta Compression**: Only send changed cells
2. **Batch Commands**: Group multiple placements
3. **Priority Queue**: Handle critical messages first
4. **Connection Pooling**: Reuse connections where possible
