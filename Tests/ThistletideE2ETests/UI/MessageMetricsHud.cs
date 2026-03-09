namespace NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// Displays message/throughput metrics in a HUD component.
/// </summary>
public class MessageMetricsHud : HudComponent
{
    private readonly int _totalSent;
    private readonly int _totalReceived;
    private readonly long _minLatency;
    private readonly long _maxLatency;
    private readonly long _avgLatency;
    private readonly double _throughput;

    public MessageMetricsHud(
        int totalSent,
        int totalReceived,
        long minLatency,
        long maxLatency,
        long avgLatency,
        double throughput,
        HudComponentConfig? config = null)
        : base(config)
    {
        _totalSent = totalSent;
        _totalReceived = totalReceived;
        _minLatency = minLatency;
        _maxLatency = maxLatency;
        _avgLatency = avgLatency;
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
            sb.AppendLine(Center("MESSAGES", Config.Width));
            sb.AppendLine(CreateSeparator());
        }

        if (Config.ShowTotal)
        {
            sb.AppendLine($"  Total Sent: {_totalSent}");
            sb.AppendLine($"  Total Received: {_totalReceived}");
        }

        if (Config.ShowMin || Config.ShowMax || Config.ShowAvg)
        {
            var latencyParts = new List<string>();
            if (Config.ShowMin) latencyParts.Add($"min={_minLatency}");
            if (Config.ShowMax) latencyParts.Add($"max={_maxLatency}");
            if (Config.ShowAvg) latencyParts.Add($"avg={_avgLatency}");
            sb.AppendLine($"  Latency (ms): {string.Join(", ", latencyParts)}");
        }

        sb.AppendLine($"  Throughput: {FormatNumber(_throughput)} msg/sec");

        return sb.ToString();
    }
}
