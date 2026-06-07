.PHONY: clean, pytest

clean:
	find . -type d \( -name "obj" -o -name "bin" \) -exec rm -rf {} +
	find . -type d -name "__pycache__" -exec rm -rf {} +
	rm -f ./ExampleAgents/*_pb2*.py

pytest:
	dotnet build LevelBasedForaging.slnx -c Release
	python -m grpc_tools.protoc -I./src/LevelBasedForaging.GrpcServer/Protos --python_out=./ExampleAgents --grpc_python_out=./ExampleAgents ./src/LevelBasedForaging.GrpcServer/Protos/foraging.proto
	pytest tests/PythonIntegration/ --server-bin=src/LevelBasedForaging.GrpcServer/bin/Release/net10.0/LevelBasedForaging.GrpcServer