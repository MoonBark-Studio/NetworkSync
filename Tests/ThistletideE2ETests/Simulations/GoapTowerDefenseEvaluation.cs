namespace NetworkSync.Tests.ThistletideE2E.Simulations;

/// <summary>
/// Self-contained GOAP evaluation for tower defense game balance and progression.
/// This implementation doesn't require external MoonBark.AI dependencies.
/// </summary>
public class GoapTowerDefenseEvaluation
{
    // Simulated world state
    private int _gridWidth = 50;
    private int _gridHeight = 50;
    private HashSet<(int x, int y)> _occupiedCells = new();
    private Dictionary<int, (int x, int y, string type)> _structures = new();
    private int _nextStructureId = 1;
    
    // Agent state
    private (int x, int y) _agentPosition = (25, 25);
    private int _wallsBuilt;
    private int _towersBuilt;
    private int _buildAttempts;
    private int _successfulBuilds;
    private int _removals;
    
    // Performance metrics
    private int _totalPlansGenerated;
    private double _totalPlanningTimeMs;
    private int _totalActionsExecuted;
    
    public async Task RunEvaluationAsync(int agentCount = 10, int stepsPerAgent = 100)
    {
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("GOAP TOWER DEFENSE EVALUATION");
        Console.WriteLine("Testing GOAP methodology for game balance and progression");
        Console.WriteLine("=".PadRight(70, '='));
        
        Console.WriteLine($"\nConfiguration:");
        Console.WriteLine($"  Grid Size: {_gridWidth}x{_gridHeight}");
        Console.WriteLine($"  Agent Count: {agentCount}");
        Console.WriteLine($"  Steps per Agent: {stepsPerAgent}");
        
        // Pre-populate some structures for realism
        PrepopulateWorld();
        
        Console.WriteLine($"\nInitial World State:");
        Console.WriteLine($"  Pre-placed structures: {_structures.Count}");
        Console.WriteLine($"  Occupied cells: {_occupiedCells.Count}");
        
        // Run simulation for multiple agents
        var agentResults = new List<AgentResult>();
        
        for (int agentId = 0; agentId < agentCount; agentId++)
        {
            var agent = new TowerDefenseGoapAgent(agentId, _gridWidth, _gridHeight);
            var result = await SimulateAgentAsync(agent, stepsPerAgent);
            agentResults.Add(result);
            
            Console.WriteLine($"\nAgent {agentId}:");
            Console.WriteLine($"  Walls built: {result.WallsBuilt}");
            Console.WriteLine($"  Towers built: {result.TowersBuilt}");
            Console.WriteLine($"  Planning time: {result.AvgPlanningTimeMs:F2}ms");
            Console.WriteLine($"  Plan length: {result.AvgPlanLength:F1} actions");
        }
        
        // Analyze results
        PrintAnalysis(agentResults);
    }
    
    private void PrepopulateWorld()
    {
        // Add some initial structures to simulate mid-game state
        var random = new Random(42);
        
        // Place fewer structures - only 5% occupancy to leave room for building
        for (int i = 0; i < 10; i++)
        {
            int x = random.Next(_gridWidth);
            int y = random.Next(_gridHeight);
            if (!IsOccupied(x, y))
            {
                PlaceStructure(x, y, "Wall");
            }
        }
        
        // Place fewer towers
        for (int i = 0; i < 5; i++)
        {
            int x = random.Next(_gridWidth);
            int y = random.Next(_gridHeight);
            if (!IsOccupied(x, y))
            {
                PlaceStructure(x, y, "Tower");
            }
        }
    }
    
    private async Task<AgentResult> SimulateAgentAsync(TowerDefenseGoapAgent agent, int steps)
    {
        var result = new AgentResult { AgentId = agent.AgentId };
        var agentPos = (x: 25, y: 25); // Start at center
        
        for (int step = 0; step < steps; step++)
        {
            // Get current world state for this agent's position
            var worldState = GetWorldState(agentPos);
            
            // Generate GOAP plan
            var planStart = DateTime.Now;
            var plan = agent.Plan(worldState);
            var planTime = (DateTime.Now - planStart).TotalMilliseconds;
            
            result.TotalPlanningTimeMs += planTime;
            _totalPlansGenerated++;
            
            // Execute plan actions
            foreach (var action in plan)
            {
                var actionResult = ExecuteAction(action, agent, ref agentPos);
                result.ActionsExecuted++;
                _totalActionsExecuted++;
                
                if (actionResult.Success)
                {
                    result.SuccessfulActions++;
                }
                
                // Small delay to simulate real-time
                await Task.Delay(1);
            }
            
            result.AvgPlanLength = (double)result.ActionsExecuted / (step + 1);
        }
        
        result.AvgPlanningTimeMs = result.TotalPlanningTimeMs / steps;
        result.WallsBuilt = agent.WallsBuilt;
        result.TowersBuilt = agent.TowersBuilt;
        
        _totalPlanningTimeMs += result.TotalPlanningTimeMs;
        
        return result;
    }
    
    private Dictionary<string, bool> GetWorldState((int x, int y) agentPos)
    {
        return new Dictionary<string, bool>
        {
            ["has_wall_nearby"] = HasStructureNearby("Wall", 5, agentPos),
            ["has_tower_nearby"] = HasStructureNearby("Tower", 5, agentPos),
            ["at_map_center"] = IsNearCenter(5, agentPos),
            ["has_space_to_build"] = HasFreeSpaceNearby(3, agentPos),
            ["too_many_walls"] = _wallsBuilt >= 10,
            ["need_defense"] = _towersBuilt < _wallsBuilt / 3,
            ["map_crowded"] = _occupiedCells.Count > _gridWidth * _gridHeight * 0.3
        };
    }
    
    private bool HasStructureNearby(string type, int radius, (int x, int y) pos)
    {
        foreach (var (id, (x, y, sType)) in _structures)
        {
            if (sType == type && Math.Abs(x - pos.x) <= radius && Math.Abs(y - pos.y) <= radius)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool IsNearCenter(int threshold, (int x, int y) pos)
    {
        int centerX = _gridWidth / 2;
        int centerY = _gridHeight / 2;
        return Math.Abs(pos.x - centerX) <= threshold && 
               Math.Abs(pos.y - centerY) <= threshold;
    }
    
    private bool HasFreeSpaceNearby(int radius, (int x, int y) pos)
    {
        int count = 0;
        for (int x = pos.x - radius; x <= pos.x + radius; x++)
        {
            for (int y = pos.y - radius; y <= pos.y + radius; y++)
            {
                if (IsValidPosition(x, y) && !IsOccupied(x, y))
                {
                    count++;
                }
            }
        }
        return count >= 3;
    }
    
    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
    }
    
    private bool IsOccupied(int x, int y)
    {
        return _occupiedCells.Contains((x, y));
    }
    
    private ActionResult ExecuteAction(GoapAction action, TowerDefenseGoapAgent agent, ref (int x, int y) agentPos)
    {
        _buildAttempts++;
        
        switch (action.Type)
        {
            case "move_north":
                if (IsValidPosition(agentPos.x, agentPos.y - 1) && !IsOccupied(agentPos.x, agentPos.y - 1))
                {
                    agentPos.y--;
                    return new ActionResult { Success = true };
                }
                break;
                
            case "move_south":
                if (IsValidPosition(agentPos.x, agentPos.y + 1) && !IsOccupied(agentPos.x, agentPos.y + 1))
                {
                    agentPos.y++;
                    return new ActionResult { Success = true };
                }
                break;
                
            case "move_east":
                if (IsValidPosition(agentPos.x + 1, agentPos.y) && !IsOccupied(agentPos.x + 1, agentPos.y))
                {
                    agentPos.x++;
                    return new ActionResult { Success = true };
                }
                break;
                
            case "move_west":
                if (IsValidPosition(agentPos.x - 1, agentPos.y) && !IsOccupied(agentPos.x - 1, agentPos.y))
                {
                    agentPos.x--;
                    return new ActionResult { Success = true };
                }
                break;
                
            case "build_wall":
                if (HasFreeSpaceNearby(1, agentPos))
                {
                    // Find a free spot
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = agentPos.x + dx;
                            int ny = agentPos.y + dy;
                            if (IsValidPosition(nx, ny) && !IsOccupied(nx, ny))
                            {
                                PlaceStructure(nx, ny, "Wall");
                                agent.WallsBuilt++;
                                _successfulBuilds++;
                                return new ActionResult { Success = true };
                            }
                        }
                    }
                }
                break;
                
            case "build_tower":
                if (HasFreeSpaceNearby(1, agentPos))
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = agentPos.x + dx;
                            int ny = agentPos.y + dy;
                            if (IsValidPosition(nx, ny) && !IsOccupied(nx, ny))
                            {
                                PlaceStructure(nx, ny, "Tower");
                                agent.TowersBuilt++;
                                _successfulBuilds++;
                                return new ActionResult { Success = true };
                            }
                        }
                    }
                }
                break;
                
            case "remove_structure":
                // Find and remove nearby structure
                foreach (var (id, (x, y, type)) in _structures.ToList())
                {
                    if (Math.Abs(x - agentPos.x) <= 2 && Math.Abs(y - agentPos.y) <= 2)
                    {
                        RemoveStructure(id);
                        _removals++;
                        return new ActionResult { Success = true };
                    }
                }
                break;
        }
        
        return new ActionResult { Success = false };
    }
    
    private void PlaceStructure(int x, int y, string type)
    {
        int id = _nextStructureId++;
        _structures[id] = (x, y, type);
        _occupiedCells.Add((x, y));
        
        if (type == "Wall") _wallsBuilt++;
        if (type == "Tower") _towersBuilt++;
    }
    
    private void RemoveStructure(int id)
    {
        if (_structures.TryGetValue(id, out var pos))
        {
            _occupiedCells.Remove((pos.x, pos.y));
            _structures.Remove(id);
        }
    }
    
    private void PrintAnalysis(List<AgentResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("GOAP EVALUATION RESULTS");
        Console.WriteLine("=".PadRight(70, '='));
        
        // Aggregate statistics
        int totalWalls = results.Sum(r => r.WallsBuilt);
        int totalTowers = results.Sum(r => r.TowersBuilt);
        double avgPlanningTime = _totalPlanningTimeMs / _totalPlansGenerated;
        double successRate = results.Sum(r => r.SuccessfulActions) * 100.0 / Math.Max(1, results.Sum(r => r.ActionsExecuted));
        
        Console.WriteLine($"\n[Build Statistics]");
        Console.WriteLine($"  Total walls built: {totalWalls}");
        Console.WriteLine($"  Total towers built: {totalTowers}");
        Console.WriteLine($"  Wall/Tower ratio: {(totalTowers > 0 ? (double)totalWalls / totalTowers : 0):F2}");
        Console.WriteLine($"  Build success rate: {_successfulBuilds * 100.0 / Math.Max(1, _buildAttempts):F1}%");
        
        Console.WriteLine($"\n[Planning Performance]");
        Console.WriteLine($"  Total plans generated: {_totalPlansGenerated}");
        Console.WriteLine($"  Average planning time: {avgPlanningTime:F2}ms");
        Console.WriteLine($"  Total actions executed: {_totalActionsExecuted}");
        
        Console.WriteLine($"\n[Action Success Rate]");
        Console.WriteLine($"  Overall success rate: {successRate:F1}%");
        
        Console.WriteLine();
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("GOAP METHODOLOGY ASSESSMENT FOR GAME BALANCE");
        Console.WriteLine("=".PadRight(70, '='));
        
        // Evaluate GOAP strengths and weaknesses
        Console.WriteLine($"\n[Strengths]");
        Console.WriteLine("  + Flexible goal-driven behavior adapts to game state");
        Console.WriteLine("  + Clear separation of goals, actions, and planning");
        Console.WriteLine("  + Agents can reason about complex placement decisions");
        Console.WriteLine("  + Easy to add new actions without breaking existing logic");
        
        Console.WriteLine($"\n[Weaknesses]");
        Console.WriteLine($"  - Planning overhead: {avgPlanningTime:F2}ms per plan (can be costly)");
        Console.WriteLine($"  - Success rate of {successRate:F1}% indicates some inefficiency");
        Console.WriteLine("  - No built-in economy/weight system for action selection");
        Console.WriteLine("  - Requires careful tuning of preconditions and effects");
        
        Console.WriteLine($"\n[Recommendations for Game Balance]");
        
        // Analyze the build patterns
        if (totalWalls > 0 && totalTowers > 0)
        {
            var ratio = (double)totalWalls / totalTowers;
            if (ratio > 3)
            {
                Console.WriteLine($"  ! Warning: High wall/tower ratio ({ratio:F1}) - agents favor walls");
                Console.WriteLine("    Consider adjusting goal costs to encourage tower placement");
            }
            else if (ratio < 1)
            {
                Console.WriteLine($"  ! Warning: Low wall/tower ratio ({ratio:F1}) - agents favor towers");
                Console.WriteLine("    Consider adding strategic value to walls");
            }
            else
            {
                Console.WriteLine($"  ✓ Balanced wall/tower ratio ({ratio:F1})");
            }
        }
        
        Console.WriteLine($"\n[Conclusion]");
        if (successRate > 70 && avgPlanningTime < 10)
        {
            Console.WriteLine("  ✓ GOAP IS VIABLE for tower defense game balance");
            Console.WriteLine("    The methodology provides good decision-making with acceptable performance");
        }
        else if (successRate > 50)
        {
            Console.WriteLine("  ~ GOAP NEEDS OPTIMIZATION for tower defense");
            Console.WriteLine("    Consider: action costs, goal prioritization, or hybrid approach");
        }
        else
        {
            Console.WriteLine("  ✗ GOAP MAY NOT BE SUITABLE in current form");
            Console.WriteLine("    High failure rate suggests need for more robust planning");
        }
        
        Console.WriteLine();
    }
    
    private class AgentResult
    {
        public int AgentId { get; set; }
        public int WallsBuilt { get; set; }
        public int TowersBuilt { get; set; }
        public double TotalPlanningTimeMs { get; set; }
        public double AvgPlanningTimeMs { get; set; }
        public int ActionsExecuted { get; set; }
        public int SuccessfulActions { get; set; }
        public double AvgPlanLength { get; set; }
    }
    
    private class ActionResult
    {
        public bool Success { get; set; }
    }
}

/// <summary>
/// Simplified GOAP agent for tower defense without external dependencies
/// </summary>
public class TowerDefenseGoapAgent
{
    public int AgentId { get; }
    public int WallsBuilt { get; set; }
    public int TowersBuilt { get; set; }
    
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    
    public TowerDefenseGoapAgent(int agentId, int gridWidth, int gridHeight)
    {
        AgentId = agentId;
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;
    }
    
    /// <summary>
    /// Generate a GOAP plan based on current world state
    /// </summary>
    public List<GoapAction> Plan(Dictionary<string, bool> worldState)
    {
        var plan = new List<GoapAction>();
        var random = new Random(AgentId * 1000 + Environment.TickCount);
        
        // Evaluate goals and prioritize
        var goals = EvaluateGoals(worldState);
        
        foreach (var (goal, priority) in goals.OrderByDescending(g => g.priority))
        {
            var goalPlan = TryGoal(goal, worldState, random);
            if (goalPlan.Count > 0)
            {
                plan.AddRange(goalPlan);
                break;
            }
        }
        
        // If no goal-based plan, do exploratory action
        if (plan.Count == 0)
        {
            plan.Add(RandomMovementAction(random));
        }
        
        return plan;
    }
    
    private List<(string goal, int priority)> EvaluateGoals(Dictionary<string, bool> state)
    {
        var goals = new List<(string, int)>();
        
        // Priority 1: Build defense if needed (or always to test)
        if (state.GetValueOrDefault("need_defense", false) || state.GetValueOrDefault("has_space_to_build", false))
        {
            goals.Add(("build_tower", 100));
            goals.Add(("build_wall", 90));
        }
        
        // Priority 2: Build walls if we don't have too many
        if (!state.GetValueOrDefault("too_many_walls", false) && state.GetValueOrDefault("has_space_to_build", false))
        {
            goals.Add(("build_wall", 80));
        }
        
        // Priority 3: Reposition if crowded
        if (state.GetValueOrDefault("map_crowded", false))
        {
            goals.Add(("find_space", 60));
        }
        
        // Priority 4: Explore if at center (to find new build spots)
        if (state.GetValueOrDefault("at_map_center", false))
        {
            goals.Add(("explore", 40));
        }
        
        // Fallback: always try to build something
        if (goals.Count == 0)
        {
            goals.Add(("build_any", 50));
        }
        
        return goals;
    }
    
    private List<GoapAction> TryGoal(string goal, Dictionary<string, bool> state, Random random)
    {
        var plan = new List<GoapAction>();
        
        switch (goal)
        {
            case "build_tower":
                // Check if we can build directly
                if (state.GetValueOrDefault("has_space_to_build", false))
                {
                    plan.Add(new GoapAction { Type = "build_tower", Cost = 5 });
                }
                else
                {
                    // Move to find space
                    plan.Add(RandomMovementAction(random));
                    plan.Add(new GoapAction { Type = "build_tower", Cost = 5 });
                }
                break;
                
            case "build_wall":
                if (state.GetValueOrDefault("has_space_to_build", false))
                {
                    plan.Add(new GoapAction { Type = "build_wall", Cost = 3 });
                }
                else
                {
                    plan.Add(RandomMovementAction(random));
                    plan.Add(new GoapAction { Type = "build_wall", Cost = 3 });
                }
                break;
                
            case "build_any":
                // Alternate between tower and wall
                if (random.Next(2) == 0)
                {
                    plan.Add(new GoapAction { Type = "build_tower", Cost = 5 });
                }
                else
                {
                    plan.Add(new GoapAction { Type = "build_wall", Cost = 3 });
                }
                break;
                
            case "find_space":
            case "explore":
                // Multiple movements
                for (int i = 0; i < 3; i++)
                {
                    plan.Add(RandomMovementAction(random));
                }
                
                // Then try to build
                plan.Add(new GoapAction { Type = "build_wall", Cost = 3 });
                break;
        }
        
        return plan;
    }
    
    private GoapAction RandomMovementAction(Random random)
    {
        var movements = new[] { "move_north", "move_south", "move_east", "move_west" };
        return new GoapAction 
        { 
            Type = movements[random.Next(movements.Length)], 
            Cost = 1 
        };
    }
}

public class GoapAction
{
    public string Type { get; set; } = "";
    public int Cost { get; set; } = 1;
}
