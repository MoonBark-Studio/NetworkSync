namespace NetworkSync.Tests.ThistletideE2E.Simulations;

/// <summary>
/// Generates placement commands with configurable patterns.
/// </summary>
public class PlacementCommandGenerator
{
    private readonly Random _random;
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly string _defaultStructureType;

    /// <summary>
    /// Pattern for generating placement positions.
    /// </summary>
    public enum PlacementPattern
    {
        /// <summary>Random positions within grid bounds.</summary>
        Random,
        
        /// <summary>Sequential positions from origin.</summary>
        Sequential,
        
        /// <summary>Positions clustered in the center.</summary>
        Centered,
        
        /// <summary>Positions along the edges.</summary>
        Edges,
        
        /// <summary>Diagonal pattern from corner to corner.</summary>
        Diagonal
    }

    public PlacementCommandGenerator(int gridWidth, int gridHeight, int randomSeed = 42, string defaultStructureType = "Wall")
    {
        _random = new Random(randomSeed);
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
        _defaultStructureType = defaultStructureType;
    }

    /// <summary>
    /// Generates the next placement position based on the pattern.
    /// </summary>
    public (int x, int y, string structureType) GenerateNext(PlacementPattern pattern, int iteration)
    {
        return pattern switch
        {
            PlacementPattern.Random => GenerateRandom(),
            PlacementPattern.Sequential => GenerateSequential(iteration),
            PlacementPattern.Centered => GenerateCentered(iteration),
            PlacementPattern.Edges => GenerateEdges(iteration),
            PlacementPattern.Diagonal => GenerateDiagonal(iteration),
            _ => GenerateRandom()
        };
    }

    /// <summary>
    /// Generates random positions.
    /// </summary>
    public (int x, int y, string structureType) GenerateRandom()
    {
        return (
            _random.Next(0, _gridWidth),
            _random.Next(0, _gridHeight),
            _defaultStructureType
        );
    }

    /// <summary>
    /// Generates sequential positions filling the grid.
    /// </summary>
    public (int x, int y, string structureType) GenerateSequential(int iteration)
    {
        int x = iteration % _gridWidth;
        int y = iteration / _gridWidth;
        
        if (y >= _gridHeight)
        {
            // Wrap around
            y = _random.Next(0, _gridHeight);
            x = _random.Next(0, _gridWidth);
        }
        
        return (x, y, _defaultStructureType);
    }

    /// <summary>
    /// Generates positions clustered around the center.
    /// </summary>
    public (int x, int y, string structureType) GenerateCentered(int iteration)
    {
        int centerX = _gridWidth / 2;
        int centerY = _gridHeight / 2;
        int radius = Math.Min(_gridWidth, _gridHeight) / 4;
        
        double angle = _random.NextDouble() * 2 * Math.PI;
        double distance = _random.NextDouble() * radius;
        
        int x = centerX + (int)(Math.Cos(angle) * distance);
        int y = centerY + (int)(Math.Sin(angle) * distance);
        
        // Clamp to grid bounds
        x = Math.Clamp(x, 0, _gridWidth - 1);
        y = Math.Clamp(y, 0, _gridHeight - 1);
        
        return (x, y, _defaultStructureType);
    }

    /// <summary>
    /// Generates positions along the grid edges.
    /// </summary>
    public (int x, int y, string structureType) GenerateEdges(int iteration)
    {
        int side = iteration % 4;
        int position = _random.Next(0, Math.Max(_gridWidth, _gridHeight));
        
        int x = side switch
        {
            0 => position,           // Top
            2 => position,           // Bottom
            _ => 0                   // Left/Right
        };
        
        int y = side switch
        {
            1 => position,           // Right
            3 => position,           // Left
            _ => 0                   // Top/Bottom
        };
        
        // Clamp
        x = Math.Clamp(x, 0, _gridWidth - 1);
        y = Math.Clamp(y, 0, _gridHeight - 1);
        
        return (x, y, _defaultStructureType);
    }

    /// <summary>
    /// Generates positions along a diagonal.
    /// </summary>
    public (int x, int y, string structureType) GenerateDiagonal(int iteration)
    {
        int step = iteration % Math.Max(_gridWidth, _gridHeight);
        
        // Alternate between main diagonal and anti-diagonal
        bool mainDiagonal = (iteration / Math.Max(_gridWidth, _gridHeight)) % 2 == 0;
        
        int x = mainDiagonal ? step : (_gridWidth - 1 - step);
        int y = step;
        
        // Clamp
        x = Math.Clamp(x, 0, _gridWidth - 1);
        y = Math.Clamp(y, 0, _gridHeight - 1);
        
        return (x, y, _defaultStructureType);
    }

    /// <summary>
    /// Generates a batch of placement commands.
    /// </summary>
    public IEnumerable<(int x, int y, string structureType)> GenerateBatch(
        PlacementPattern pattern, 
        int batchSize,
        int startIteration = 0)
    {
        for (int i = 0; i < batchSize; i++)
        {
            yield return GenerateNext(pattern, startIteration + i);
        }
    }
}
