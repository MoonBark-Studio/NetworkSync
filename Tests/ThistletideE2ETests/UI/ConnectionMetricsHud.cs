namespace NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// Displays connection metrics in a HUD component.
/// </summary>
public class ConnectionMetricsHud : HudComponent
{
    private readonly int _totalConnections;
    private readonly int _successfulConnections;
    private readonly int _failedConnections;
    private readonly long _minConnectionTime;
    private readonly long _maxConnectionTime;
    private readonly long _avgConnectionTime;

    public ConnectionMetricsHud(
        int totalConnections,
        int successfulConnections,
        int failedConnections,
        long minConnectionTime,
        long maxConnectionTime,
        long avgConnectionTime,
        HudComponentConfig? config = null) 
        : base(config)
    {
        _totalConnections = totalConnections;
        _successfulConnections = successfulConnections;
        _failedConnections = failedConnections;
        _minConnectionTime = minConnectionTime;
        _maxConnectionTime = maxConnectionTime;
        _avgConnectionTime = avgConnectionTime;
    }

    public override void Render() => Console.WriteLine(RenderToString());

    public override string RenderToString()
    {
        var sb = new System.Text.StringBuilder();
        
        if (Config.ShowHeaders)
        {
            sb.AppendLine();
            sb.AppendLine(CreateSeparator());
            sb.AppendLine(Center("CONNECTIONS", Config.Width));
            sb.AppendLine(CreateSeparator());
        }

        if (Config.ShowTotal)
        {
            sb.AppendLine($"  Total: {_totalConnections}");
            sb.AppendLine($"  Successful: {_successfulConnections}");
            sb.AppendLine($"  Failed: {_failedConnections}");
        }

        if (Config.ShowMin || Config.ShowMax || Config.ShowAvg)
        {
            var timeParts = new List<string>();
            if (Config.ShowMin) timeParts.Add($"min={_minConnectionTime}");
            if (Config.ShowMax) timeParts.Add($"max={_maxConnectionTime}");
            if (Config.ShowAvg) timeParts.Add($"avg={_avgConnectionTime}");
            sb.AppendLine($"  Connection Time (ms): {string.Join(", ", timeParts)}");
        }

        return sb.ToString();
    }
}
