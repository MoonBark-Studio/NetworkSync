using System.Diagnostics;

namespace MoonBark.NetworkSync.Tests.ThistletideE2E.Simulations;

/// <summary>
/// Base class for all test simulations.
/// Provides common functionality for lifecycle management and metrics collection.
/// </summary>
public abstract class SimulationScenario
{
    protected readonly Stopwatch _stopwatch = new();
    private bool _isInitialized;
    private bool _isRunning;

    /// <summary>Gets whether the simulation has been initialized.</summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>Gets whether the simulation is currently running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>Gets the elapsed time since the simulation started.</summary>
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    /// <summary>
    /// Initializes the simulation.
    /// Override in derived classes to set up resources.
    /// </summary>
    public virtual Task InitializeAsync()
    {
        _isInitialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the simulation.
    /// Override in derived classes to begin execution.
    /// </summary>
    public virtual Task StartAsync()
    {
        _stopwatch.Start();
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses the simulation.
    /// </summary>
    public virtual Task PauseAsync()
    {
        _stopwatch.Stop();
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resumes a paused simulation.
    /// </summary>
    public virtual Task ResumeAsync()
    {
        _stopwatch.Start();
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the simulation.
    /// Override in derived classes to clean up resources.
    /// </summary>
    public virtual Task StopAsync()
    {
        _stopwatch.Stop();
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up all resources.
    /// Override in derived classes to dispose resources.
    /// </summary>
    public virtual Task DisposeAsync()
    {
        _stopwatch.Stop();
        _isRunning = false;
        _isInitialized = false;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration for simulation scenarios.
/// </summary>
public class SimulationConfig
{
    /// <summary>Duration of the simulation in seconds.</summary>
    public int DurationSeconds { get; set; } = 30;

    /// <summary>Grid width for placement simulations.</summary>
    public int GridWidth { get; set; } = 1000;

    /// <summary>Grid height for placement simulations.</summary>
    public int GridHeight { get; set; } = 1000;

    /// <summary>Network port for server simulations.</summary>
    public int Port { get; set; } = 7777;

    /// <summary>Maximum number of connections.</summary>
    public int MaxConnections { get; set; } = 100;

    /// <summary>Number of target connections/clients.</summary>
    public int TargetConnections { get; set; } = 10;

    /// <summary>Batch size for client connections.</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Delay between batches in milliseconds.</summary>
    public int BatchDelayMs { get; set; } = 100;

    /// <summary>Random seed for reproducibility.</summary>
    public int RandomSeed { get; set; } = 42;
}

/// <summary>
/// Result of a simulation run.
/// </summary>
public class SimulationResult
{
    /// <summary>Whether the simulation completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Duration of the simulation.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Error message if the simulation failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Additional metadata about the simulation run.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
