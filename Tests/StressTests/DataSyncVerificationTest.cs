using MoonBark.NetworkSync.Core.Messages;
using MoonBark.NetworkSync.Core.Services;
using MoonBark.NetworkSync.Tests.StressTests.Infrastructure;
using MoonBark.NetworkSync.Tests.StressTests.Mocks;

namespace MoonBark.NetworkSync.Tests.StressTests;

/// <summary>
/// End-to-end tests for verifying data synchronization between server and clients.
/// Tests that Thistletide placement data is correctly synced across all connected clients.
/// </summary>
public class DataSyncVerificationTest
{
    private readonly int _port;

    public DataSyncVerificationTest(int port = 7779)
    {
        _port = port;
    }

    /// <summary>
    /// Tests that all clients receive the same placement data after server processes commands.
    /// </summary>
    public async Task<SyncVerificationResult> VerifyDataSyncAsync(int clientCount = 10, int placementsPerClient = 5)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"DATA SYNC VERIFICATION TEST");
        Console.WriteLine($"Clients: {clientCount}, Placements/Client: {placementsPerClient}");
        Console.WriteLine(new string('=', 60));

        var server = new StressTestServer(_port, clientCount + 10);
        server.Start();
        await Task.Delay(500);

        var clients = new List<StressTestClient>();
        var syncResults = new SyncVerificationResult();

        try
        {
            // Connect all clients
            Console.WriteLine("\n[SyncTest] Connecting clients...");
            for (int i = 0; i < clientCount; i++)
            {
                var client = new StressTestClient("127.0.0.1", _port);
                var connected = await client.ConnectAsync(i + 1, timeoutMs: 5000);

                if (connected)
                {
                    clients.Add(client);
                    Console.WriteLine($"[SyncTest] Client {i + 1} connected");
                }
                else
                {
                    Console.WriteLine($"[SyncTest] Client {i + 1} failed to connect");
                }
            }

            Console.WriteLine($"\n[SyncTest] Connected: {clients.Count}/{clientCount}");

            // Have each client place structures at unique positions
            Console.WriteLine("\n[SyncTest] Sending placement commands...");
            var random = new Random(123);
            var allPlacements = new List<(int x, int y)>();

            for (int i = 0; i < placementsPerClient; i++)
            {
                foreach (var client in clients)
                {
                    var x = random.Next(0, 100);
                    var y = random.Next(0, 100);

                    // Skip duplicates
                    if (allPlacements.Contains((x, y))) continue;
                    allPlacements.Add((x, y));

                    await client.SendPlacementCommandAsync(x, y, "TestStructure");
                    syncResults.TotalCommands++;
                }
            }

            // Wait for replication
            Console.WriteLine("\n[SyncTest] Waiting for replication...");
            await Task.Delay(2000);

            // Verify server state
            var serverPlacements = server.GetAllPlacements();
            syncResults.ServerPlacementCount = serverPlacements.Count;
            Console.WriteLine($"[SyncTest] Server has {serverPlacements.Count} placements");

            // Verify each client's state
            var clientStates = new List<Dictionary<(int x, int y), Core.Services.CellOccupancyData>>();
            for (int i = 0; i < clients.Count; i++)
            {
                var client = clients[i];
                var localPlacements = client.GetLocalPlacements();
                clientStates.Add(localPlacements);
                Console.WriteLine($"[SyncTest] Client {i + 1} has {localPlacements.Count} placements");
            }

            // Compare client states with server
            Console.WriteLine("\n[SyncTest] Verifying sync consistency...");

            for (int i = 0; i < clients.Count; i++)
            {
                var clientPlacements = clientStates[i];
                var mismatches = 0;

                // Check server placements exist in client
                foreach (var serverPlacement in serverPlacements)
                {
                    if (!clientPlacements.ContainsKey(serverPlacement.Key))
                    {
                        mismatches++;
                        syncResults.MissingOnClient++;
                    }
                }

                // Check client placements exist on server
                foreach (var clientPlacement in clientPlacements)
                {
                    if (!serverPlacements.ContainsKey(clientPlacement.Key))
                    {
                        mismatches++;
                        syncResults.ExtraOnClient++;
                    }
                }

                if (mismatches > 0)
                {
                    Console.WriteLine($"[SyncTest] ⚠ Client {i + 1} has {mismatches} mismatches");
                    syncResults.ClientsWithMismatches++;
                }
                else
                {
                    Console.WriteLine($"[SyncTest] ✓ Client {i + 1} perfectly synced");
                }
            }

            // Check consistency between clients
            Console.WriteLine("\n[SyncTest] Verifying client-to-client consistency...");
            for (int i = 0; i < clients.Count; i++)
            {
                for (int j = i + 1; j < clients.Count; j++)
                {
                    var state1 = clientStates[i];
                    var state2 = clientStates[j];

                    var differences = 0;
                    foreach (var kvp in state1)
                    {
                        if (!state2.ContainsKey(kvp.Key))
                            differences++;
                    }
                    foreach (var kvp in state2)
                    {
                        if (!state1.ContainsKey(kvp.Key))
                            differences++;
                    }

                    if (differences > 0)
                    {
                        Console.WriteLine($"[SyncTest] ⚠ Client {i + 1} and Client {j + 1} differ by {differences}");
                        syncResults.ClientToClientMismatches += differences;
                    }
                }
            }

            syncResults.TotalClients = clients.Count;
            syncResults.Success = syncResults.MissingOnClient == 0 && 
                                  syncResults.ExtraOnClient == 0 && 
                                  syncResults.ClientToClientMismatches == 0;

            // Print summary
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("SYNC VERIFICATION RESULT");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"  Total Commands Sent: {syncResults.TotalCommands}");
            Console.WriteLine($"  Server Placements: {syncResults.ServerPlacementCount}");
            Console.WriteLine($"  Missing on Client: {syncResults.MissingOnClient}");
            Console.WriteLine($"  Extra on Client: {syncResults.ExtraOnClient}");
            Console.WriteLine($"  Client-to-Client Mismatches: {syncResults.ClientToClientMismatches}");
            Console.WriteLine($"  Clients with Mismatches: {syncResults.ClientsWithMismatches}");
            Console.WriteLine($"  SUCCESS: {syncResults.Success}");
            Console.WriteLine(new string('=', 60));

            return syncResults;
        }
        finally
        {
            foreach (var client in clients)
            {
                try { await client.DisconnectAsync(); client.Dispose(); } catch { }
            }
            server.Stop();
            server.Dispose();
        }
    }

    /// <summary>
    /// Tests data sync under high load with many concurrent placements.
    /// </summary>
    public async Task<SyncVerificationResult> VerifyDataSyncUnderLoadAsync(int clientCount = 20, int concurrentPlacements = 100)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine($"DATA SYNC UNDER LOAD TEST");
        Console.WriteLine($"Clients: {clientCount}, Concurrent Placements: {concurrentPlacements}");
        Console.WriteLine(new string('=', 60));

        var server = new StressTestServer(_port + 1, clientCount + 10);
        server.Start();
        await Task.Delay(500);

        var clients = new List<StressTestClient>();
        var syncResults = new SyncVerificationResult();

        try
        {
            // Connect clients
            Console.WriteLine("\n[LoadTest] Connecting clients...");
            for (int i = 0; i < clientCount; i++)
            {
                var client = new StressTestClient("127.0.0.1", _port + 1);
                if (await client.ConnectAsync(i + 1, timeoutMs: 5000))
                {
                    clients.Add(client);
                }
            }
            Console.WriteLine($"[LoadTest] Connected: {clients.Count}");

            // Send concurrent placements
            Console.WriteLine($"\n[LoadTest] Sending {concurrentPlacements} concurrent placements...");
            var random = new Random(456);
            var tasks = new List<Task<bool>>();

            for (int i = 0; i < concurrentPlacements; i++)
            {
                var client = clients[random.Next(clients.Count)];
                var x = random.Next(0, 200);
                var y = random.Next(0, 200);
                tasks.Add(client.SendPlacementCommandAsync(x, y, "LoadTestStructure").ContinueWith(t => t.Result.success));
            }

            await Task.WhenAll(tasks);
            var successfulPlacements = tasks.Count(t => t.Result);
            Console.WriteLine($"[LoadTest] Successful placements: {successfulPlacements}/{concurrentPlacements}");

            // Wait for replication
            Console.WriteLine("\n[LoadTest] Waiting for replication...");
            await Task.Delay(3000);

            // Verify sync
            var serverPlacements = server.GetAllPlacements();
            syncResults.ServerPlacementCount = serverPlacements.Count;

            foreach (var client in clients)
            {
                var clientPlacements = client.GetLocalPlacements();
                var mismatches = 0;

                foreach (var sp in serverPlacements)
                {
                    if (!clientPlacements.ContainsKey(sp.Key))
                        mismatches++;
                }

                if (mismatches > 0)
                {
                    syncResults.MissingOnClient += mismatches;
                    syncResults.ClientsWithMismatches++;
                }
            }

            syncResults.TotalClients = clients.Count;
            syncResults.TotalCommands = concurrentPlacements;
            syncResults.Success = syncResults.MissingOnClient < serverPlacements.Count * 0.1; // Allow 10% tolerance under load

            Console.WriteLine($"\n[LoadTest] Server placements: {serverPlacements.Count}");
            Console.WriteLine($"[LoadTest] Missing on clients: {syncResults.MissingOnClient}");
            Console.WriteLine($"[LoadTest] Clients with issues: {syncResults.ClientsWithMismatches}");
            Console.WriteLine($"[LoadTest] SUCCESS: {syncResults.Success}");

            return syncResults;
        }
        finally
        {
            foreach (var client in clients)
            {
                try { await client.DisconnectAsync(); client.Dispose(); } catch { }
            }
            server.Stop();
            server.Dispose();
        }
    }
}

public class SyncVerificationResult
{
    public int TotalClients { get; set; }
    public int TotalCommands { get; set; }
    public int ServerPlacementCount { get; set; }
    public int MissingOnClient { get; set; }
    public int ExtraOnClient { get; set; }
    public int ClientToClientMismatches { get; set; }
    public int ClientsWithMismatches { get; set; }
    public bool Success { get; set; }

    public override string ToString()
    {
        return $"""
            Sync Verification Result:
              Clients: {TotalClients}
              Commands: {TotalCommands}
              Server Placements: {ServerPlacementCount}
              Missing on Client: {MissingOnClient}
              Extra on Client: {ExtraOnClient}
              Client-to-Client Mismatches: {ClientToClientMismatches}
              Clients with Issues: {ClientsWithMismatches}
              Success: {Success}
            """;
    }
}
