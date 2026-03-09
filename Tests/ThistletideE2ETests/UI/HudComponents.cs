namespace NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// Configuration for HUD display components.
/// </summary>
public class HudComponentConfig
{
    /// <summary>Width of the HUD section.</summary>
    public int Width { get; set; } = 60;
    
    /// <summary>Show section headers.</summary>
    public bool ShowHeaders { get; set; } = true;
    
    /// <summary>Show minimum values.</summary>
    public bool ShowMin { get; set; } = true;
    
    /// <summary>Show maximum values.</summary>
    public bool ShowMax { get; set; } = true;
    
    /// <summary>Show average values.</summary>
    public bool ShowAvg { get; set; } = true;
    
    /// <summary>Show total counts.</summary>
    public bool ShowTotal { get; set; } = true;
    
    /// <summary>Number of decimal places for floating point values.</summary>
    public int DecimalPlaces { get; set; } = 2;
    
    /// <summary>Color codes for console output (if supported).</summary>
    public bool UseColors { get; set; } = false;
}

/// <summary>
/// Base class for HUD display components.
/// </summary>
public abstract class HudComponent
{
    protected HudComponentConfig Config { get; }

    protected HudComponent(HudComponentConfig? config = null)
    {
        Config = config ?? new HudComponentConfig();
    }

    /// <summary>
    /// Renders the component to the console.
    /// </summary>
    public abstract void Render();

    /// <summary>
    /// Renders the component to a string.
    /// </summary>
    public abstract string RenderToString();

    protected string FormatNumber(double value) => value.ToString($"F{Config.DecimalPlaces}");
    protected string Center(string text, int width) => text.PadLeft((width + text.Length) / 2).PadRight(width);
    protected string CreateSeparator() => new string('=', Config.Width);
}

/// <summary>
/// Displays timing metrics in a HUD component.
/// </summary>
public class TimingMetricsHud : HudComponent
{
    private readonly TimeSpan _elapsedTime;

    public TimingMetricsHud(TimeSpan elapsedTime, HudComponentConfig? config = null)
        : base(config)
    {
        _elapsedTime = elapsedTime;
    }

    public override void Render() => Console.WriteLine(RenderToString());

    public override string RenderToString()
    {
        var sb = new System.Text.StringBuilder();
        
        if (Config.ShowHeaders)
        {
            sb.AppendLine();
            sb.AppendLine(CreateSeparator());
            sb.AppendLine(Center("TIMING", Config.Width));
            sb.AppendLine(CreateSeparator());
        }

        sb.AppendLine($"  Test Duration: {_elapsedTime:hh\\:mm\\:ss\\.fff}");
        return sb.ToString();
    }
}
