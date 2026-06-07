import pytest


def pytest_addoption(parser):
    """Register a custom command-line option for the C# server binary."""
    parser.addoption(
        "--server-bin",
        action="store",
        default=None,
        help="Path to the pre-compiled C# GrpcServer executable binary",
    )


@pytest.fixture(scope="session")
def server_bin_path(request):
    """A fixture that retrieves the executable path, falling back to an environment variable if not specified via CLI."""
    import os

    path = request.config.getoption("--server-bin")
    if path:
        return os.path.abspath(path)

    # Fallback option: Check an environment variable
    env_path = os.getenv("FORAGING_SERVER_BIN")
    if env_path:
        return os.path.abspath(env_path)

    # Raise an error if the tester forgot to supply the path
    pytest.fail(
        "Missing C# server binary path! Please provide it via command line: "
        "pytest --server-bin=path/to/binary OR set the FORAGING_SERVER_BIN environment variable."
    )
