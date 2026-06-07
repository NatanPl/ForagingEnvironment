using Xunit;
using Agent;
using LevelBasedForaging.Core;

namespace LevelBasedForaging.Tests {
    public class CSharpAgentIntegrationTests {
        [Fact]
        public void ExampleAgent_CanCompleteFullEpisodeWithoutCrashing() {
            int w = 5, h = 5, objects = 10, agents = 5;
            var env = new ForagingEnvironment(h, w, objects, agents);
            var agent = new ExampleAgent(w, h, agents);
            
            var state = env.Reset();
            int maxSteps = 500;
            int currentStep = 0;

            var exception = Record.Exception(() => {
                while (!env.Done() && currentStep < maxSteps)
                {
                    var actions = agent.GetActions(state);
                    var result = env.PerformActions(actions);
                    agent.Reward(result.Reward);
                    state = result.NextState;
                    currentStep++;
                }
            });

            Assert.Null(exception); // Ensure no runtime errors occurred
            Assert.True(env.Done() || currentStep == maxSteps);
        }
    }
}