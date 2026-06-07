import subprocess
import time

import foraging_pb2
import foraging_pb2_grpc
import grpc
from agent import RandomPythonAgent


def test_python_grpc_loop_execution(server_bin_path):

    cs_command = [
        server_bin_path,
        "--width",
        "5",
        "--height",
        "5",
        "--objects",
        "10",
        "--agents",
        "5",
        "--port",
        "50051",
    ]
    server_process = subprocess.Popen(cs_command)
    time.sleep(1)
    try:
        channel = grpc.insecure_channel("localhost:50051")
        env = foraging_pb2_grpc.ForagingServiceStub(channel)

        agent = RandomPythonAgent(5, 5, num_agents=5)

        state = env.Reset(foraging_pb2.Empty())
        assert len(state.agent_locations) == 5

        actions = agent.get_actions(state)
        assert len(actions) == 5

        step_result = env.Step(foraging_pb2.ActionRequest(actions=actions))

        assert step_result is not None
        assert hasattr(step_result, "reward")
        assert hasattr(step_result, "next_state")
        assert hasattr(step_result, "done")

    finally:
        server_process.terminate()
        server_process.wait()
