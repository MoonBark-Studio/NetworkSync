namespace NetworkSync.Tests.ThistletideE2E;

/// <summary>
/// E2E Tests for Thistletide network sync using Thistletide occupancy map API.
/// Tests the NetworkSync plugin with Thistletide's TrackedPlacementOccupancyMap.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("THISTLETIDE E2E NETWORK SYNC TESTS");
        Console.WriteLine("Testing with Thistletide TrackedPlacementOccupancyMap API");
        Console.WriteLine("=".PadRight(60, '='));

        var results = new List<ThistletideStressTestResult>();

        try
        {
            // Test with 64 connections
            Console.WriteLine();
            Console.WriteLine("[1/2] Running 64-connection stress test...");
            var runner64 = new ThistletideStressTestRunner(
                targetConnections: 64,
                port: 7777,
                gridSize: 1000
            );
            var result64 = await runner64.RunTestAsync(
                durationSeconds: 20,
                batchSize: 16,
                batchDelayMs: 50
            );
            results.Add(result64);

            await Task.Delay(2000);

            // Test with 128 connections
            Console.WriteLine();
            Console.WriteLine("[2/2] Running 128-connection stress test...");
            var runner128 = new ThistletideStressTestRunner(
                targetConnections: 128,
                port: 7778,
                gridSize: 1000
            );
            var result128 = await runner128.RunTestAsync(
                durationSeconds: 20,
                batchSize: 16,
                batchDelayMs: 50
            );
            results.Add(result128);

            // Print final summary
            PrintFinalSummary(results);

            // Determine exit code
            var allPassed = results.All(r => r.Success);
            Console.WriteLine();
            Console.WriteLine(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED");
            
            return allPassed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("FATAL ERROR: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void PrintFinalSummary(List<ThistletideStressTestResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("#".PadRight(60, '#'));
        Console.WriteLine("FINAL TEST SUMMARY - THISTLETIDE E2E WITH REAL GAME DATA");
        Console.WriteLine("#".PadRight(60, '#'));

        foreach (var result in results)
        {
            Console.WriteLine();
            Console.WriteLine("--- " + result.TargetConnections + " Connections ---");
            Console.WriteLine(result);
        }

        // Analyze limits
        Console.WriteLine();
        Console.WriteLine("#".PadRight(60, '#'));
        Console.WriteLine("PERFORMANCE LIMITS ANALYSIS");
        Console.WriteLine("#".PadRight(60, '#'));

        var r64 = results.FirstOrDefault(r => r.TargetConnections == 64);
        var r128 = results.FirstOrDefault(r => r.TargetConnections == 128);

        if (r64 != null && r128 != null)
        {
            var latencyGrowth = r128.AvgLatencyMs > 0 
                ? (double)(r128.AvgLatencyMs - r64.AvgLatencyMs) / r64.AvgLatencyMs * 100 
                : 0;

            Console.WriteLine();
            Console.WriteLine("[Latency Analysis]");
            Console.WriteLine("  64 connections avg latency:  " + r64.AvgLatencyMs + "ms");
            Console.WriteLine("  128 connections avg latency: " + r128.AvgLatencyMs + "ms");
            Console.WriteLine("  Latency growth: " + latencyGrowth.ToString("F1") + "%");

            var throughputDrop = r64.MessagesPerSecond > 0
                ? (r64.MessagesPerSecond - r128.MessagesPerSecond) / r64.MessagesPerSecond * 100
                : 0;

            Console.WriteLine();
            Console.WriteLine("[Throughput Analysis]");
            Console.WriteLine("  64 connections: " + r64.MessagesPerSecond.ToString("F2") + " msg/sec");
            Console.WriteLine("  128 connections: " + r128.MessagesPerSecond.ToString("F2") + " msg/sec");
            Console.WriteLine("  Throughput drop: " + throughputDrop.ToString("F1") + "%");

            Console.WriteLine();
            Console.WriteLine("[Sync Quality]");
            Console.WriteLine("  64 connections - Mismatches: " + r64.SyncMismatches);
            Console.WriteLine("  128 connections - Mismatches: " + r128.SyncMismatches);
        }

        Console.WriteLine();
        Console.WriteLine("#".PadRight(60, '#'));
        Console.WriteLine("RECOMMENDED LIMITS");
        Console.WriteLine("#".PadRight(60, '#'));

        // Determine safe limits based on results
        int recommendedMaxConnections = 64;
        long maxAcceptableLatency = 100; // ms

        if (r64 != null && r128 != null)
        {
            if (r128.AvgLatencyMs < maxAcceptableLatency && r128.SyncMismatches == 0)
            {
                recommendedMaxConnections = 128;
            }
            else if (r128.AvgLatencyMs >= maxAcceptableLatency)
            {
                recommendedMaxConnections = 64;
            }
            else
            {
                // Find the threshold
                recommendedMaxConnections = 64;
            }
        }

        Console.WriteLine("  Recommended Max Connections: " + recommendedMaxConnections);
        Console.WriteLine("  Max Acceptable Latency: " + maxAcceptableLatency + "ms");
        Console.WriteLine("  Note: These limits apply to Thistletide's TrackedPlacementOccupancyMap");
        Console.WriteLine("        with delta-based replication on LiteNetLib");
    }
}
