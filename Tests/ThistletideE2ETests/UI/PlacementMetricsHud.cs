namespace NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// Displays placement metrics in a HUD component.
/// </summary>
public class PlacementMetricsHud : HudComponent
{
    private readonly int _totalCommands;
    private readonly int _successfulPlacements;
    private readonly int _failedPlacements;
    private readonly int _serverPlacements;
    private readonly int _syncMismatches;
    private readonly double _throughput;

    public PlacementMetricsHud(
        int totalCommands,
        int successfulPlacements,
        int failedPlacements,
        int serverPlacements,
        int syncMismatches,
        double throughput,
        HudComponentConfig? config = null)
        : base(config)
    {
        _totalCommands = totalCommands;
        _successfulPlacements = successfulPlacements;
        _failedPlacements = failedPlacements;
        _serverPlacements = serverPlacements;
        _syncMismatches = syncMismatches;
        _throughput = throughput;
    }

    public override void Render() => Console.WriteLine(RenderToString());

    public override string RenderToString()
    {
        var sb = new System.Text.StringBuilder();
        
        if (Config.ShowHeaders)
        {
            sb.AppendLine();
            sb.AppendLine(CreateSeparator());
            sb.AppendLine(Center("PLACEMENTS", Config.Width));
            sb.AppendLine(CreateSeparator());
        }

        if (Config.ShowTotal)
        {
            sb.AppendLine($"  Total Commands: {_totalCommands}");
            sb.AppendLine($"  Successful: {_successfulPlacements}");
            sb.AppendLine($"  Failed: {_failedPlacements}");
        }

        sb.AppendLine($"  Server Placements: {_serverPlacements}");
        sb.AppendLine($"  Sync Mismatches: {_syncMismatches}");
        sb.AppendLine($"  Throughput: {FormatNumber(_throughput)} placements/sec");

        return sb.ToString();
    }
}
