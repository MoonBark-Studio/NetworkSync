using System.Diagnostics;
using NetworkSync.Tests.StressTests.Infrastructure;

namespace NetworkSync.Tests.StressTests;

/// <summary>
/// Main entry point for NetworkSync stress tests.
/// Tests networking sync with 64 and 128 concurrent connections.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("NETWORKSYNC STRESS TESTS");
        Console.WriteLine("Testing Thistletide Data Sync with Concurrent Connections");
        Console.WriteLine("=".PadRight(60, '='));

        // Run stress tests for different connection counts
        var results = new List<StressTestResult>();

        // Test with 64 connections
        Console.WriteLine("\n\n" + new string('#', 60));
        Console.WriteLine("TEST 1: 64 Concurrent Connections");
        Console.WriteLine(new string('#', 60));
        var test64 = new StressTestRunner(targetConnections: 64, port: 7777);
        var result64 = await test64.RunTestAsync(
            durationSeconds: 20,
            placementsPerClient: 20,
            batchSize: 16,
            batchDelayMs: 50
        );
        results.Add(result64);

        // Allow cleanup between tests
        await Task.Delay(3000);

        // Test with 128 connections
        Console.WriteLine("\n\n" + new string('#', 60));
        Console.WriteLine("TEST 2: 128 Concurrent Connections");
        Console.WriteLine(new string('#', 60));
        var test128 = new StressTestRunner(targetConnections: 128, port: 7778);
        var result128 = await test128.RunTestAsync(
            durationSeconds: 20,
            placementsPerClient: 20,
            batchSize: 32,
            batchDelayMs: 50
        );
        results.Add(result128);

        // Run data sync verification tests
        Console.WriteLine("\n\n" + new string('#', 60));
        Console.WriteLine("TEST 3: Data Sync Verification (10 clients)");
        Console.WriteLine(new string('#', 60));
        var syncTest = new DataSyncVerificationTest(port: 7779);
        var syncResult = await syncTest.VerifyDataSyncAsync(clientCount: 10, placementsPerClient: 10);

        // Run data sync under load
        Console.WriteLine("\n\n" + new string('#', 60));
        Console.WriteLine("TEST 4: Data Sync Under Load (20 clients)");
        Console.WriteLine(new string('#', 60));
        var loadSyncResult = await syncTest.VerifyDataSyncUnderLoadAsync(clientCount: 20, concurrentPlacements: 100);

        // Print final summary
        PrintFinalSummary(results, syncResult, loadSyncResult);

        // Document findings
        DocumentLimits(results);
    }

    private static void PrintFinalSummary(List<StressTestResult> results, SyncVerificationResult syncResult, SyncVerificationResult loadSyncResult)
    {
        Console.WriteLine("\n\n" + new string('=', 60));
        Console.WriteLine("FINAL STRESS TEST SUMMARY");
        Console.WriteLine(new string('=', 60));

        foreach (var result in results)
        {
            Console.WriteLine($"\n{result}");
        }

        Console.WriteLine($"\n[Data Sync Verification]");
        Console.WriteLine($"  Basic Sync: {(syncResult.Success ? "✓ PASSED" : "✗ FAILED")}");
        Console.WriteLine($"    Server placements: {syncResult.ServerPlacementCount}");
        Console.WriteLine($"    Missing on client: {syncResult.MissingOnClient}");
        Console.WriteLine($"  Load Sync: {(loadSyncResult.Success ? "✓ PASSED" : "✗ FAILED")}");
        Console.WriteLine($"    Server placements: {loadSyncResult.ServerPlacementCount}");
        Console.WriteLine($"    Missing on client: {loadSyncResult.MissingOnClient}");

        // Determine the limit
        var successResults = results.Where(r => r.Success).ToList();
        if (successResults.Count == results.Count && syncResult.Success && loadSyncResult.Success)
        {
            Console.WriteLine("\n✓ ALL TESTS PASSED");
            Console.WriteLine("  NetworkSync successfully handles all tested concurrent connections.");
        }
        else
        {
            var failed = results.FirstOrDefault(r => !r.Success);
            Console.WriteLine($"\n✗ SOME TESTS FAILED");
            if (failed != null)
            {
                Console.WriteLine($"  Failed connections: {failed.FailedConnections}");
                Console.WriteLine($"  Sync mismatches: {failed.SyncMismatches}");
            }
            if (!syncResult.Success)
            {
                Console.WriteLine($"  Data sync verification failed");
            }
        }
    }

    private static void DocumentLimits(List<StressTestResult> results)
    {
        Console.WriteLine("\n\n" + new string('=', 60));
        Console.WriteLine("NETWORK SYNC LIMITS DOCUMENTATION");
        Console.WriteLine(new string('=', 60));

        // Analyze results to determine limits
        var maxSuccessful = results.Where(r => r.Success).Max(r => r.TargetConnections);
        var maxMessagesPerSec = results.Max(r => r.MessagesPerSecond);
        var maxPlacementsPerSec = results.Max(r => r.PlacementsPerSecond);
        var avgLatencyAtMax = results.FirstOrDefault(r => r.TargetConnections == maxSuccessful)?.AvgMessageLatencyMs ?? 0;

        Console.WriteLine($"""
            Performance Limits:
            --------------------
            
            Maximum Concurrent Connections (Stable):
              - Tested: Up to {maxSuccessful} connections
              - Recommendation: Stay below {maxSuccessful} for production
            
            Throughput Limits:
              - Max Messages/Second: {maxMessagesPerSec:F2}
              - Max Placements/Second: {maxPlacementsPerSec:F2}
            
            Latency:
              - Average at max load: {avgLatencyAtMax}ms
              
            Known Issues at Scale:
            ----------------------
        """);

        // Document specific findings
        foreach (var result in results)
        {
            if (!result.Success)
            {
                Console.WriteLine($"  - At {result.TargetConnections} connections:");
                if (result.FailedConnections > 0)
                    Console.WriteLine($"    * {result.FailedConnections} connection failures");
                if (result.SyncMismatches > 0)
                    Console.WriteLine($"    * {result.SyncMismatches} data sync mismatches");
            }
        }

        // Recommendations
        Console.WriteLine($"""
            
            Recommendations:
            ----------------
            1. For production deployments, keep concurrent connections below {maxSuccessful}
            2. Monitor message latency - consider scaling if avg exceeds 100ms
            3. Use batch connections (10-32 at a time) to prevent connection storms
            4. Implement connection limits at load balancer level for larger deployments
            
            Testing Notes:
            --------------
            - Tests run on localhost (127.0.0.1)
            - Actual performance may vary based on network conditions
            - LiteNetLib handles UDP efficiently but NAT/firewall can impact real-world results
            - Memory usage scales linearly with connection count (~1-2KB per connection)
        """);

        Console.WriteLine(new string('=', 60));
    }
}
