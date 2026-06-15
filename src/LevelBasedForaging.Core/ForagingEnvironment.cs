namespace LevelBasedForaging.Core {
    public interface IAgent {
        List<AgentAction> GetActions(State state);
        void Reward(double reward);
    }
    public enum AgentAction { North = 0, South = 1, West = 2, East = 3 }
    public record State(IReadOnlyList<(int X, int Y)> AgentLocations, IReadOnlyList<(int X, int Y, int Level)> ObjectLocations);
    public record HistoryFrame(State State, double Reward);
    public class ForagingEnvironment {
        internal readonly int height;
        internal readonly int width;
        internal readonly int numObjects;
        internal readonly int numAgents;
        private readonly bool storeHistory;
        private readonly Random rnd;
        internal (int X, int Y)[] agentLocations;
        internal (int X, int Y, int Level)[] objectLocations;
        internal int steps;
        internal int activeObjects;
        internal readonly Dictionary<(int X, int Y), int> agentCount = [];
        
        public List<HistoryFrame> History { get; private set; } = [];

        public ForagingEnvironment(int height, int width, int objects, int agents, int? seed = null, bool storeHistory = true) {
            this.height = height;
            this.width = width;
            this.numObjects = objects;
            this.numAgents = agents;
            this.storeHistory = storeHistory;

            if (objects > height * width) {
                throw new ArgumentException("Number of objects cannot exceed total grid cells.");
            }
            if (agents < 1) {
                throw new ArgumentException("There must be at least one agent.");
            }
            if (objects < 1) {
                throw new ArgumentException("There must be at least one object.");
            }
            if (objects + agents > height * width) {
                throw new ArgumentException("Total number of agents and objects cannot exceed total grid cells.");
            }
            
            this.rnd = seed.HasValue ? new Random(seed.Value) : new Random();
            this.agentLocations = new (int X, int Y)[agents];
            this.objectLocations = new (int X, int Y, int Level)[objects];
            this.agentCount.EnsureCapacity(agents);
            Reset();
        }

        public State Reset() {
            steps = 0;

            var usedPositions = new HashSet<(int, int)>();

            int currentObjects = 0;
            while (currentObjects < numObjects) {
                int i = rnd.Next(0, height);
                int j = rnd.Next(0, width);
                if (!usedPositions.Contains((i, j))) {
                    currentObjects++;
                    int object_level = rnd.Next(1, numAgents + 1); 
                    objectLocations[currentObjects - 1] = (i, j, object_level);
                    usedPositions.Add((i, j));
                }
            }
            activeObjects = numObjects;

            int currentAgents = 0;
            while (currentAgents < numAgents) {
                int i = rnd.Next(0, height);
                int j = rnd.Next(0, width);
                if (!usedPositions.Contains((i, j))) {
                    currentAgents++;
                    agentLocations[currentAgents - 1] = (i, j);
                    usedPositions.Add((i, j));
                }
            }

            var state = new State([.. agentLocations], [.. objectLocations[..activeObjects]]);

            if (storeHistory) {
                History = [new(state, 0)];
            }

            return state;
        }

        private double UpdateWorld() {
            steps++;
            agentCount.Clear();

            // Count agents on each location
            foreach (var ag in agentLocations) {
                agentCount.TryGetValue(ag, out int count);
                agentCount[ag] = count + 1;
            }

            double reward = 0;
            for (int i = 0; i < activeObjects; i++) {
                var (X, Y, Level) = objectLocations[i];
                agentCount.TryGetValue((X, Y), out int count);
                if (count >= Level) {
                    reward += Level;
                    objectLocations[i] = objectLocations[activeObjects - 1]; // Move last object to current position
                    objectLocations[activeObjects - 1] = (0, 0, 0); // Clear the last object
                    activeObjects--; // Decrease the count of active objects
                    i--; // Stay on the same index to check the new object that was moved here
                }
            }

            return reward - 0.1; // 0.1 penalty per step
        }

        public (double Reward, State NextState, bool Done) PerformActions(List<AgentAction> actions) {
            if (actions.Count != numAgents) {
                throw new ArgumentException("Number of actions must match number of agents.");
            }

            for (int i = 0; i < numAgents; i++) {
                int currentX = agentLocations[i].X;
                int currentY = agentLocations[i].Y;
                AgentAction act = actions[i];

                int nextX = currentX;
                int nextY = currentY;

                switch (act) {
                    case AgentAction.North: nextX = Math.Max(currentX - 1, 0); break;
                    case AgentAction.South: nextX = Math.Min(currentX + 1, height - 1); break;
                    case AgentAction.West:  nextY = Math.Max(currentY - 1, 0); break;
                    case AgentAction.East:  nextY = Math.Min(currentY + 1, width - 1); break;
                    default: throw new ArgumentException("Invalid action.");
                }

                agentLocations[i] = (nextX, nextY);
            }

            double r = UpdateWorld();

            var state = new State([.. agentLocations], [.. objectLocations[..activeObjects]]);

            if (storeHistory) {
                History.Add(new HistoryFrame(state, r));
            }

            return (r, state, Done());
        }

        public void ClearHistory() => History.Clear();
        public bool Done() => activeObjects == 0 || steps >= 500;
        public void PrintState(State state) {
            int[,,] grid = new int[2, height, width];
            
            foreach (var (X, Y) in state.AgentLocations) {
                grid[0, X, Y] += 1;
            }
            foreach (var (X, Y, Level) in state.ObjectLocations) {
                grid[1, X, Y] += Level;
            }

            const int cellWidth = 5;

            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    int agents = grid[0, i, j];
                    int objLevel = grid[1, i, j];
                    
                    string cellText;

                    if (agents > 0 && objLevel > 0) {
                        cellText = $"{agents}/{objLevel}";
                        Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    } else if (agents > 0) {
                        cellText = agents.ToString();
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                    } else if (objLevel > 0) {
                        cellText = objLevel.ToString();
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                    } else {
                        cellText = ".";
                        Console.BackgroundColor = ConsoleColor.Black;
                    }

                    // Defensive formatting, ensuring cell text centering and consistent width
                    int totalPadding = cellWidth - cellText.Length;
                    int padLeft = totalPadding / 2 + cellText.Length;
                    
                    string formattedCell = cellText.PadLeft(padLeft).PadRight(cellWidth);

                    Console.Write(formattedCell);
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }

        public void RenderHistory(int speed = 200) {
            double totalScore = 0;
            int stepCounter = 0;

            foreach (var frame in History) {
                Console.Clear();
                totalScore += frame.Reward;
                Console.WriteLine($"Step: {stepCounter++} | Score: {totalScore:F1}");
                
                PrintState(frame.State);
                Thread.Sleep(speed);
            }
        }
    }
}