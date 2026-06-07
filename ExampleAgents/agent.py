import random
import subprocess
import sys
import time
from argparse import ArgumentParser

import foraging_pb2
import foraging_pb2_grpc
import grpc

argument_parser = ArgumentParser()

### C# environment arguments
if sys.platform.startswith("win"):
    default_executable = "./ForagingEnv.exe"
else:
    default_executable = "./ForagingEnv"

argument_parser.add_argument("--executable", type=str, default=default_executable)
argument_parser.add_argument("--address", type=str, default="localhost")
argument_parser.add_argument("--port", type=int, default=50051)

### Agent arguments
argument_parser.add_argument("--width", type=int, default=5)
argument_parser.add_argument("--height", type=int, default=5)
argument_parser.add_argument("--num_objects", type=int, default=10)
argument_parser.add_argument("--num_agents", type=int, default=5)
argument_parser.add_argument("--num_episodes", type=int, default=5)


class RandomPythonAgent:
    def __init__(self, width, height, num_agents):
        self.width = width
        self.height = height
        self.num_agents = num_agents

    def get_actions(self, state):
        # 0=North, 1=South, 2=West, 3=East
        return [random.randint(0, 3) for _ in range(self.num_agents)]

    def reward(self, reward):
        pass


def main(args):
    print("Starting the C# environment")
    cs_command = [
        args.executable,
        "--width",
        str(args.width),
        "--height",
        str(args.height),
        "--objects",
        str(args.num_objects),
        "--agents",
        str(args.num_agents),
        "--port",
        str(args.port),
    ]
    cs_process = subprocess.Popen(cs_command)

    # give the C# environment some time to start up
    time.sleep(1)

    try:
        channel = grpc.insecure_channel(args.address + ":" + str(args.port))
        env = foraging_pb2_grpc.ForagingServiceStub(channel)

        agent = RandomPythonAgent(args.width, args.height, args.num_agents)

        for episode in range(args.num_episodes):
            state = env.Reset(foraging_pb2.Empty())
            total_reward = 0
            done = False

            while not done:
                actions = agent.get_actions(state)

                action_req = foraging_pb2.ActionRequest(actions=actions)
                step_result = env.Step(action_req)
                agent.reward(step_result.reward)

                total_reward += step_result.reward
                state = step_result.next_state
                done = step_result.done

            print(f"Episode {episode + 1} finished with reward: {total_reward:.2f}")

    except Exception as e:
        print(f"An error occurred: {e}")
    finally:
        print("Terminating the C# environment")
        cs_process.terminate()
        cs_process.wait()


if __name__ == "__main__":
    args = argument_parser.parse_args()
    main(args)
