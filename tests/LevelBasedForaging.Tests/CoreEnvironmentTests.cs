using Xunit;
using LevelBasedForaging.Core;
using System.Collections.Generic;
using System.Linq;

namespace LevelBasedForaging.Tests {
    public class CoreEnvironmentTests {
        [Theory]
        [InlineData(2, 2, 5, 1)] // Objects > cells
        [InlineData(5, 5, 1, 0)] // Agents < 1
        [InlineData(5, 5, 0, 1)] // Objects < 1
        [InlineData(2, 2, 3, 2)] // Objects + Agents > cells
        public void Constructor_WithInvalidArguments_ShouldThrowArgumentException(
            int height, int width, int objects, int agents) {
                Assert.Throws<ArgumentException>(() => 
                new ForagingEnvironment(height, width, objects, agents));
            }
        
        [Theory]
        [InlineData(2, 1, 1, 1)]
        [InlineData(1, 2, 1, 1)]
        [InlineData(5, 5, 5, 2)]
        [InlineData(5, 5, 10, 5)]
        [InlineData(10, 10, 20, 5)]
        [InlineData(100, 100, 500, 50)]
        public void Constructor_WithValidArguments_ShouldInitializeProperly(
            int height, int width, int objects, int agents) {
                var env = new ForagingEnvironment(height, width, objects, agents);
                Assert.NotNull(env);
                Assert.Single(env.History);
            }
        
        [Fact]
        public void Reset_WithSeed_ShouldBeDeterministic() {
            int seed = 42;
            var env1 = new ForagingEnvironment(5, 5, 5, 2, seed);
            var env2 = new ForagingEnvironment(5, 5, 5, 2, seed);

            var history1 = env1.History.First();
            var history2 = env2.History.First();

            Assert.Equal(history1.State.ObjectLocations, history2.State.ObjectLocations);
            Assert.Equal(history1.Reward, history2.Reward);

            var state1 = env1.Reset();
            var state2 = env2.Reset();

            Assert.Equal(state1.ObjectLocations, state2.ObjectLocations);
            Assert.Equal(state1.AgentLocations, state2.AgentLocations);
            
            Assert.True(env1.agentLocations.SequenceEqual(env2.agentLocations));
            Assert.True(env1.objectLocations.SequenceEqual(env2.objectLocations));
        }
        [Theory]
        [InlineData(2, 1, 1, 1)]
        [InlineData(5, 5, 10, 5)]
        [InlineData(100, 100, 500, 50)]
        public void Reset_ShouldPopulateWorldWithCorrectAmountOfEntities(
            int height, int width, int objects, int agents) {
                var env = new ForagingEnvironment(height, width, objects, agents);
                var state = env.Reset();

                Assert.Equal(agents, state.AgentLocations.Count);
                Assert.Equal(objects, state.ObjectLocations.Count);
                Assert.Equal(objects, env.activeObjects);
            }

        [Fact]
        public void Reset_ShouldAssignObjectLevelsWithinValidRange() {
            int agents = 500;
            var env = new ForagingEnvironment(1000, 1000, 500, agents);
            env.Reset();

            for(int i = 0; i < env.activeObjects; i++) {
                Assert.InRange(env.objectLocations[i].Level, 1, agents);
            }
        }
    
    
        
        [Theory]
        [InlineData(AgentAction.North, 0, 0)]
        [InlineData(AgentAction.West, 0, 0)]
        [InlineData(AgentAction.South, 4, 4)]
        [InlineData(AgentAction.East, 4, 4)]
        public void PerformActions_AgainstBoundaries_ShouldNotMoveOutOfBounds(
            AgentAction action, int X, int Y) {
                var env = new ForagingEnvironment(5, 5, 1, 1);
                env.Reset();
                env.agentLocations[0] = (X, Y); // Place agent at the corner

                var (_, _, _) = env.PerformActions([action]);
                var newLocation = env.agentLocations[0];

                Assert.Equal((X, Y), newLocation);  // Agent should not move out of bounds
            }
        
        [Fact]
        public void PerformActions_ShouldApplyStepPenalty() {
            var env = new ForagingEnvironment(5, 5, 1, 1);
            env.agentLocations[0] = (0,0);
            env.objectLocations[0] = (2,2,1); // in the middle so we don't interact with it

            var (reward1, _, _) = env.PerformActions([AgentAction.South]);
            var (reward2, _, _) = env.PerformActions([AgentAction.North]);

            Assert.Equal(-0.1, reward1);
            Assert.Equal(-0.1, reward2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void ForageShouldApplyRewardsCorrectly(int Count) {
            var env = new ForagingEnvironment(5, 5, 1, Count);
            for (int i = 0; i < Count; i++) {
                env.agentLocations[i] = (0,0);
            }
            env.objectLocations[0] = (0,1, Count);
            var (reward, _, _) = env.PerformActions([.. Enumerable.Repeat(AgentAction.East, Count)]);
            
            Assert.Equal(Count - 0.1, reward);
        }

        [Fact]
        public void OverkillDoesntBenefit() {
            var env = new ForagingEnvironment(5, 5, 1, 5);
            for (int i = 0; i < 5; i++) {
                env.agentLocations[i] = (0,0);
            }
            env.objectLocations[0] = (0,1, 3); // Level 3 object

            var (reward, _, _) = env.PerformActions([.. Enumerable.Repeat(AgentAction.East, 5)]);
            Assert.Equal(3 - 0.1, reward); // Should only get level 3 reward, not level 5
        }

        [Fact]
        public void NotEnoughAgentsDoNotHarvest() {
            var env = new ForagingEnvironment(5, 5, 1, 2);
            env.agentLocations[0] = (0,0);
            env.agentLocations[1] = (0,0);
            env.objectLocations[0] = (0,1, 3); // Level 3 object

            var (reward, _, _) = env.PerformActions([.. Enumerable.Repeat(AgentAction.East, 2)]);
            Assert.Equal(-0.1, reward); // Should not get any reward since only 2 agents are present but level 3 is required
        }

        [Fact]
        public void PerformActions_ShouldAllowSimultaneousObjectCollection() {
            var env = new ForagingEnvironment(5, 5, 2, 2); // 2 objects, 2 agents
            env.agentLocations[0] = (0, 0);
            env.agentLocations[1] = (4, 4);
            env.objectLocations[0] = (0, 1, 1); // Level 1 object near Agent 1
            env.objectLocations[1] = (4, 3, 1); // Level 1 object near Agent 2

            var (reward, state, _) = env.PerformActions([AgentAction.East, AgentAction.West]);
            
            Assert.Equal(2 - 0.1, reward); // Both level-1 objects collected simultaneously
            Assert.Empty(state.ObjectLocations);
            Assert.Equal(0, env.activeObjects);
        }

        [Fact]
        public void DoneReturnsFalse() {
            var env = new ForagingEnvironment(5, 5, 1, 1);
            Assert.False(env.Done());
            env.agentLocations[0] = (0,0);
            env.objectLocations[0] = (2,2, 1);
            Assert.False(env.Done());
            for (int i = 0; i < 499; i++) {
                env.PerformActions([AgentAction.North]);
                Assert.False(env.Done());
            }
            Assert.False(env.Done());
        }

        [Fact]
        public void DoneReturnsTrueAfter500Steps() {
            var env = new ForagingEnvironment(5, 5, 1, 1);
            env.agentLocations[0] = (0,0);
            env.objectLocations[0] = (2,2, 1);
            for (int i = 0; i < 500; i++) {
                env.PerformActions([AgentAction.North]);
            }
            Assert.True(env.Done());
        }

        [Fact]
        public void DoneReturnsTrueAfterObjectCollected() {
            var env = new ForagingEnvironment(5, 5, 2, 1);
            env.agentLocations[0] = (0,0);
            env.objectLocations[0] = (0,1, 1);
            env.objectLocations[1] = (0,2, 1);
            Assert.False(env.Done());
            
            var (reward, _, _) = env.PerformActions([AgentAction.East]);
            Assert.Equal(1 - 0.1, reward); // Collected first object
            Assert.False(env.Done());

            (reward, _, _) = env.PerformActions([AgentAction.East]);
            Assert.Equal(1 - 0.1, reward); // Collected second object
            Assert.True(env.Done());
        }

        [Fact]
        public void HistoryIsCleared() {
            var env = new ForagingEnvironment(5, 5, 1, 1);
            Assert.Single(env.History);
            env.PerformActions([AgentAction.North]);
            Assert.Equal(2, env.History.Count);
            env.ClearHistory();
            Assert.Empty(env.History);
            env.Reset();
            Assert.Single(env.History);
        }

        [Fact]
        public void PerformActions_WithMismatchedActionCount_ShouldThrowArgumentException() {
            var env = new ForagingEnvironment(5, 5, 1, 2);
            env.agentLocations[0] = (0,0);
            env.agentLocations[1] = (0,0);
            env.objectLocations[0] = (2,2, 1);

            Assert.Throws<ArgumentException>(() => 
                env.PerformActions([AgentAction.East])); // Only 1 action for 2 agents
        }

        [Fact]
        public void PerformActions_WithInvalidAction_ShouldThrowArgumentException() {
            var env = new ForagingEnvironment(5, 5, 1, 1);
            env.agentLocations[0] = (0,0);
            env.objectLocations[0] = (2,2, 1);

            Assert.Throws<ArgumentException>(() => 
                env.PerformActions([(AgentAction)999])); // Invalid action
        }
    }
}