namespace MoonBark.NetworkSync.Tests.ThistletideE2E.UI;

/// <summary>
/// UI component for displaying test results in a structured format.
/// This component can be used in various contexts (console, GUI, web).
/// </summary>
public class TestResultsUI
{
    private readonly List<TestResultSection> _sections = new();

    /// <summary>
    /// Adds a section to the results display.
    /// </summary>
    public TestResultsUI AddSection(string title, Dictionary<string, string> values)
    {
        _sections.Add(new TestResultSection { Title = title, Values = values });
        return this;
    }

    /// <summary>
    /// Renders the results to console.
    /// </summary>
    public void RenderToConsole()
    {
        foreach (var section in _sections)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(section.Title.ToUpperInvariant());
            Console.WriteLine(new string('=', 60));

            foreach (var kvp in section.Values)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
    }

    /// <summary>
    /// Renders the results as a formatted string.
    /// </summary>
    public string RenderToString()
    {
        var sb = new System.Text.StringBuilder();

        foreach (var section in _sections)
        {
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine(section.Title.ToUpperInvariant());
            sb.AppendLine(new string('=', 60));

            foreach (var kvp in section.Values)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
        }

        sb.AppendLine();
        sb.AppendLine(new string('=', 60));

        return sb.ToString();
    }

    /// <summary>
    /// Clears all sections.
    /// </summary>
    public void Clear()
    {
        _sections.Clear();
    }

    private class TestResultSection
    {
        public string Title { get; set; } = "";
        public Dictionary<string, string> Values { get; set; } = new();
    }
}

/// <summary>
/// Builder for TestResultsUI.
/// </summary>
public class TestResultsUIBuilder
{
    private readonly TestResultsUI _ui = new();

    public TestResultsUIBuilder WithTimingSection(TimeSpan elapsed)
    {
        _ui.AddSection("Timing", new Dictionary<string, string>
        {
            ["Test Duration"] = elapsed.ToString(@"hh\:mm\:ss\.fff")
        });
        return this;
    }

    public TestResultsUIBuilder WithConnectionSection(int total, int successful, int failed, long minMs, long maxMs, long avgMs)
    {
        _ui.AddSection("Connections", new Dictionary<string, string>
        {
            ["Total"] = total.ToString(),
            ["Successful"] = successful.ToString(),
            ["Failed"] = failed.ToString(),
            ["Connection Time (ms)"] = $"min={minMs}, max={maxMs}, avg={avgMs}"
        });
        return this;
    }

    public TestResultsUIBuilder WithMessageSection(int sent, int received, long minLatency, long maxLatency, long avgLatency, double throughput)
    {
        _ui.AddSection("Messages", new Dictionary<string, string>
        {
            ["Total Sent"] = sent.ToString(),
            ["Total Received"] = received.ToString(),
            ["Latency (ms)"] = $"min={minLatency}, max={maxLatency}, avg={avgLatency}",
            ["Throughput"] = $"{throughput:F2} msg/sec"
        });
        return this;
    }

    public TestResultsUIBuilder WithPlacementSection(int total, int successful, int failed, int serverPlacements, int syncMismatches, double throughput)
    {
        _ui.AddSection("Placements", new Dictionary<string, string>
        {
            ["Total Commands"] = total.ToString(),
            ["Successful"] = successful.ToString(),
            ["Failed"] = failed.ToString(),
            ["Server Placements"] = serverPlacements.ToString(),
            ["Sync Mismatches"] = syncMismatches.ToString(),
            ["Throughput"] = $"{throughput:F2} placements/sec"
        });
        return this;
    }

    public TestResultsUI Build() => _ui;
}
