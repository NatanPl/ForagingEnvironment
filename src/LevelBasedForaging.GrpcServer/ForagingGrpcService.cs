using Grpc.Core;
using System.Threading.Tasks;
using System.Linq;
using LevelBasedForaging.Core;
using LevelBasedForaging.Grpc;

public class ForagingGrpcService(ForagingEnvironment env) : ForagingService.ForagingServiceBase {
    private readonly ForagingEnvironment _env = env;

    public override Task<StateMessage> Reset(Empty request, ServerCallContext context) {
        var state = _env.Reset();
        return Task.FromResult(ConvertToProtoState(state));
    }

    public override Task<StepResponse> Step(ActionRequest request, ServerCallContext context) {
        var actions = request.Actions.Select(a => (AgentAction)a).ToList();
        
        var (Reward, NextState, Done) = _env.PerformActions(actions);

        return Task.FromResult(new StepResponse {
            Reward = Reward,
            NextState = ConvertToProtoState(NextState),
            Done = Done
        });
    }

    private StateMessage ConvertToProtoState(State state) {
        var protoState = new StateMessage();
        
        // Map Agent Locations
        protoState.AgentLocations.AddRange(
            state.AgentLocations.Select(loc => new Location { X = loc.X, Y = loc.Y })
        );

        // Map Object Locations (Entity-Centric)
        protoState.ObjectLocations.AddRange(
            state.ObjectLocations.Select(obj => new ObjectLocation { 
                X = obj.X, 
                Y = obj.Y, 
                Level = obj.Level 
            })
        );

        return protoState;
    }
}