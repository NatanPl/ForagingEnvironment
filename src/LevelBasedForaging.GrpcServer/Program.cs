using System;
using Grpc.Core;
using System.Threading;
using LevelBasedForaging.Core;
using LevelBasedForaging.Grpc;

class Program {
    static void Main(string[] args) {
        int width = 5;
        int height = 5;
        int objects = 10;
        int agents = 5;
        int port = 50051;
        int? seed = null;

        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "--width" && i + 1 < args.Length) width = int.Parse(args[++i]);
            else if (args[i] == "--height" && i + 1 < args.Length) height = int.Parse(args[++i]);
            else if (args[i] == "--objects" && i + 1 < args.Length) objects = int.Parse(args[++i]);
            else if (args[i] == "--agents" && i + 1 < args.Length) agents = int.Parse(args[++i]);
            else if (args[i] == "--port" && i + 1 < args.Length) port = int.Parse(args[++i]);
            else if (args[i] == "--seed" && i + 1 < args.Length) seed = int.Parse(args[++i]);
        }
        var env = new ForagingEnvironment(height, width, objects, agents, seed);

        Server server = new Server {
            Services = { ForagingService.BindService(new ForagingGrpcService(env)) },
            Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
        };

        server.Start();
        Console.WriteLine($"C# Environment Server listening on port {port}");
        Console.WriteLine($"Grid: {width}x{height}, Objects: {objects}, Agents: {agents}");

        Thread.Sleep(Timeout.Infinite);
    }
}