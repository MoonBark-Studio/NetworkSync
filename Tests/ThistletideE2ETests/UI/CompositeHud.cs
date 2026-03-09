namespace NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// A composite HUD that combines multiple metrics components.
/// </summary>
public class CompositeHud : HudComponent
{
    private readonly List<HudComponent> _components = new();
    private readonly string _title;

    public CompositeHud(string title, HudComponentConfig? config = null) 
        : base(config)
    {
        _title = title;
    }

    public CompositeHud AddComponent(HudComponent component)
    {
        _components.Add(component);
        return this;
    }

    public override void Render() => Console.WriteLine(RenderToString());

    public override string RenderToString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine();
        sb.AppendLine(CreateSeparator());
        sb.AppendLine(Center(_title.ToUpperInvariant(), Config.Width));
        sb.AppendLine(CreateSeparator());

        foreach (var component in _components)
        {
            sb.AppendLine(component.RenderToString());
        }

        return sb.ToString();
    }
}

/// <summary>
/// Builder for creating composite HUDs with fluent API.
/// </summary>
public class CompositeHudBuilder
{
    private readonly string _title;
    private readonly HudComponentConfig _config;
    private readonly List<HudComponent> _components = new();

    public CompositeHudBuilder(string title, HudComponentConfig? config = null)
    {
        _title = title;
        _config = config ?? new HudComponentConfig();
    }

    public CompositeHudBuilder WithTiming(TimeSpan elapsed)
    {
        _components.Add(new TimingMetricsHud(elapsed, _config));
        return this;
    }

    public CompositeHudBuilder WithConnections(
        int total, int successful, int failed,
        long minTime, long maxTime, long avgTime)
    {
        _components.Add(new ConnectionMetricsHud(
            total, successful, failed, minTime, maxTime, avgTime, _config));
        return this;
    }

    public CompositeHudBuilder WithMessages(
        int sent, int received,
        long minLatency, long maxLatency, long avgLatency, double throughput)
    {
        _components.Add(new MessageMetricsHud(
            sent, received, minLatency, maxLatency, avgLatency, throughput, _config));
        return this;
    }

    public CompositeHudBuilder WithPlacements(
        int total, int successful, int failed,
        int serverPlacements, int syncMismatches, double throughput)
    {
        _components.Add(new PlacementMetricsHud(
            total, successful, failed, serverPlacements, syncMismatches, throughput, _config));
        return this;
    }

    public CompositeHud Build()
    {
        var hud = new CompositeHud(_title, _config);
        foreach (var component in _components)
        {
            hud.AddComponent(component);
        }
        return hud;
    }
}
