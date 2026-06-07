using System;
using System.Collections.Generic;
using LevelBasedForaging.Core;

namespace Agent {
    public class ExampleAgent : IAgent {
        private readonly Random random = new Random();
        private readonly int agentCount = 0;
        public ExampleAgent(int agentCount = 0) {
            this.agentCount = agentCount;
        }
        public List<AgentAction> GetActions(State observation) {
            int count = agentCount > 0 ? agentCount : observation.AgentLocations.Count;
            return new List<AgentAction>(new AgentAction[count].Select(_ => (AgentAction)random.Next(0, 4)));
        }
        public void Reward(double reward) {}

    }

    class Program {
        static void Main(string[] args) {
            int agents = 5, objects = 10, width = 5, height = 5;
            var env = new ForagingEnvironment(height, width, objects, agents);
            var agent = new ExampleAgent(agents);

            for (int episode = 0; episode < 100; episode++) {
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
            }
        }
    }
}