using Grpc.Core;
using System.Threading.Tasks;
using System.Linq;
using LevelBasedForaging.Core;
using LevelBasedForaging.Grpc;

public class ForagingGrpcService : ForagingService.ForagingServiceBase {
    private readonly ForagingEnvironment _env;

    public ForagingGrpcService(ForagingEnvironment env) {
        _env = env;
    }

    public override Task<StateMessage> Reset(Empty request, ServerCallContext context) {
        var state = _env.Reset();
        return Task.FromResult(ConvertToProtoState(state));
    }

    public override Task<StepResponse> Step(ActionRequest request, ServerCallContext context) {
        var actions = request.Actions.Select(a => (AgentAction)a).ToList();
        
        var stepResult = _env.PerformActions(actions);

        return Task.FromResult(new StepResponse {
            Reward = stepResult.Reward,
            NextState = ConvertToProtoState(stepResult.NextState),
            Done = _env.Done()
        });
    }

    private StateMessage ConvertToProtoState(State state) {
        var protoState = new StateMessage();
        protoState.World.AddRange(state.World.Cast<int>());
        protoState.AgentLocations.AddRange(state.AgentLocations.Select(loc => new Location { X = loc.X, Y = loc.Y }));
        return protoState;
    }
}