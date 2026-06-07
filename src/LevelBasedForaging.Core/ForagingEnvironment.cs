namespace LevelBasedForaging.Core {
    public interface IAgent {
        List<AgentAction> GetActions(State state);
        
        void Reward(double reward);
    }
    public enum AgentAction { North = 0, South = 1, West = 2, East = 3 }
    public record State(int[,] World, IReadOnlyList<(int X, int Y)> AgentLocations);
    public record HistoryFrame(int[,] World, double Reward);
    public class ForagingEnvironment {
        internal readonly int height;
        internal readonly int width;
        internal readonly int numObjects;
        internal readonly int numAgents;
        
        internal List<(int X, int Y)> agentLocations = new List<(int X, int Y)>();
        internal List<(int X, int Y, int Level)> objectLocations = new List<(int X, int Y, int Level)>();
        internal int[,] world = new int[0, 0];
        internal int steps;
        
        private readonly Random rnd;
        public List<HistoryFrame> History { get; private set; } = new List<HistoryFrame>();

        public ForagingEnvironment(int height, int width, int objects, int agents, int? seed = null) {
            this.height = height;
            this.width = width;
            this.numObjects = objects;
            this.numAgents = agents;

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
            Reset();
        }

        public State Reset() {
            agentLocations = new List<(int X, int Y)>();
            objectLocations = new List<(int X, int Y, int Level)>();
            world = new int[height, width];
            steps = 0;

            int currentObjects = 0;
            while (currentObjects < numObjects) {
                int i = rnd.Next(0, height);
                int j = rnd.Next(0, width);
                if (world[i, j] == 0) {
                    currentObjects++;
                    int object_level = rnd.Next(1, numAgents + 1); 
                    world[i, j] = object_level;
                    objectLocations.Add((i, j, object_level));
                }
            }

            int currentAgents = 0;
            while (currentAgents < numAgents) {
                int i = rnd.Next(0, height);
                int j = rnd.Next(0, width);
                if (world[i, j] == 0) {
                    currentAgents++;
                    world[i, j] = -1;
                    agentLocations.Add((i, j));
                }
            }

            History = new List<HistoryFrame> { new HistoryFrame(DeepCopy(world), 0) };

            return new State(DeepCopy(world), new List<(int X, int Y)>(agentLocations));
        }

        private double UpdateWorld() {
            steps++;
            int[,] tempWorldCount = new int[height, width];

            // Count agents on each location
            foreach (var ag in agentLocations) {
                tempWorldCount[ag.X, ag.Y]++;
            }

            double reward = 0;
            // Iterate backwards to safely remove foraged items
            for (int i = objectLocations.Count - 1; i >= 0; i--) {
                var obj = objectLocations[i];
                if (tempWorldCount[obj.X, obj.Y] >= obj.Level) {
                    reward += obj.Level;
                    objectLocations.RemoveAt(i);
                }
            }

            // Rebuild actual world representation
            world = new int[height, width];
            
            foreach (var ag in agentLocations) {
                world[ag.X, ag.Y] -= 1; // Negative represents agents
            }

            foreach (var obj in objectLocations) {
                world[obj.X, obj.Y] = obj.Level; // Positive represents objects
            }

            return reward - 0.1; // 0.1 penalty per step
        }

        public (double Reward, State NextState) PerformActions(List<AgentAction> actions) {
            if (actions.Count != agentLocations.Count) {
                throw new ArgumentException("Number of actions must match number of agents.");
            }
            var newLocations = new List<(int X, int Y)>();

            for (int i = 0; i < agentLocations.Count; i++) {
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

                newLocations.Add((nextX, nextY));
            }

            agentLocations = newLocations;
            double r = UpdateWorld();

            History.Add(new HistoryFrame(DeepCopy(world), r));

            return (r, new State(DeepCopy(world), new List<(int X, int Y)>(agentLocations)));
        }

        public void ClearHistory() => History.Clear();

        public bool Done() => objectLocations.Count == 0 || steps >= 500;

        // Helper to copy the 2D array accurately
        private int[,] DeepCopy(int[,] original) {
            int[,] copy = new int[original.GetLength(0), original.GetLength(1)];
            Array.Copy(original, copy, original.Length);
            return copy;
        }

        public void PrintState(State state) {
            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    int cell = state.World[i, j];
                    if (cell < 0) {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.Write($" {-cell} "); // Agent
                    } else if (cell > 0) {
                        Console.BackgroundColor = ConsoleColor.DarkBlue;
                        Console.Write($" {cell} "); // Object
                    } else {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write(" . "); // Empty
                    }
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
                
                PrintState(new State(frame.World, new List<(int X, int Y)>(agentLocations)));
                Thread.Sleep(speed);
            }
        }
    }
}