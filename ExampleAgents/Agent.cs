using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using LevelBasedForaging.Core;

namespace Agent {
    public class ExampleAgent : IAgent {
        private readonly Random random = new Random();
        private readonly int agentCount = 0;
        private readonly int width = 0;
        private readonly int height = 0;
        public ExampleAgent(int width, int height, int agentCount) {
            this.width = width;
            this.height = height;
            this.agentCount = agentCount;
        }
        public List<AgentAction> GetActions(State observation) {
            int count = agentCount > 0 ? agentCount : observation.AgentLocations.Count;
            return new List<AgentAction>(new AgentAction[count].Select(_ => (AgentAction)random.Next(0, 4)));
        }
        public void Reward(double reward) {}
    }

    public class Options {
        [Option('a', "agents", Required = false, Default = 5, HelpText = "Number of foraging agents.")]
        public int Agents { get; set; }
        [Option('o', "objects", Required = false, Default = 10, HelpText = "Number of objects in the environment.")]
        public int Objects { get; set; }
        [Option('w', "width", Required = false, Default = 5, HelpText = "Width of the grid.")]
        public int Width { get; set; }
        [Option('h', "height", Required = false, Default = 5, HelpText = "Height of the grid.")]
        public int Height { get; set; }
        [Option('e', "episodes", Required = false, Default = 100, HelpText = "Number of episodes to simulate.")]
        public int Episodes { get; set; }
    }

    class Program {
        static void Main(string[] args) {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunSimulation);
        }
        static void RunSimulation(Options opts) {
            Console.WriteLine($"Starting simulation with {opts.Agents} agents, {opts.Objects} objects, grid size {opts.Width}x{opts.Height}, for {opts.Episodes} episodes.");
            
            var env = new ForagingEnvironment(opts.Height, opts.Width, opts.Objects, opts.Agents);
            var agent = new ExampleAgent(opts.Width, opts.Height, opts.Agents);

            var totalRewards = new List<double>();
            for (int episode = 0; episode < opts.Episodes; episode++) {
                var state = env.Reset();
                double totalReward = 0;

                while (!env.Done()) {
                    var actions = agent.GetActions(state);
                    var stepResult = env.PerformActions(actions);
                    agent.Reward(stepResult.Reward);
                    totalReward += stepResult.Reward;
                    state = stepResult.NextState;
                }
                Console.WriteLine($"Episode {episode + 1} finished with reward: {totalReward:F2}");
                totalRewards.Add(totalReward);
            }
            double averageReward = totalRewards.Average();
            double maxReward = totalRewards.Max();
            double minReward = totalRewards.Min();
            Console.WriteLine($"Simulation completed. Average Reward: {averageReward:F2}, Max Reward: {maxReward:F2}, Min Reward: {minReward:F2}");
        }
    }
}