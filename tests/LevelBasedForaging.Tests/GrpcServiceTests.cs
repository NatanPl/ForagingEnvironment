using Grpc.Core;
using Xunit;
using LevelBasedForaging.Core;
using LevelBasedForaging.Grpc;
using System.Threading.Tasks;

namespace LevelBasedForaging.Tests {
    public class ForagingGrpcServiceTests {
        private ServerCallContext GetDummyContext() {
            return null!;
        }

        [Fact]
        public async Task Reset_ShouldReturnCorrectlyFormattedStateMessage() {
            var env = new ForagingEnvironment(height: 5, width: 5, objects: 3, agents: 2);
            var service = new ForagingGrpcService(env);
            var context = GetDummyContext();

            var response = await service.Reset(new Empty(), context);

            Assert.NotNull(response);
            Assert.Equal(25, response.World.Count); // 5x5 grid flattened
            Assert.Equal(2, response.AgentLocations.Count); // 2 agents
        }

        [Fact]
        public async Task Step_ShouldExecuteActionsAndReturnValidStepResponse() {
            var env = new ForagingEnvironment(height: 5, width: 5, objects: 3, agents: 2);
            var service = new ForagingGrpcService(env);
            var context = GetDummyContext();

            await service.Reset(new Empty(), context); 

            var request = new ActionRequest();
            request.Actions.Add(0); 
            request.Actions.Add(3);

            var response = await service.Step(request, context);

            Assert.NotNull(response);
            Assert.NotNull(response.NextState);
            Assert.Equal(25, response.NextState.World.Count);
            Assert.Equal(2, response.NextState.AgentLocations.Count);
            Assert.False(response.Done);
        }
    }
}