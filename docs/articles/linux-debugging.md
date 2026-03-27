# Linux LLVM debugging

To debug Flame's LLVM backend on Linux from another OS, the repository includes a Docker image definition that installs the tooling needed to reproduce the Linux CI setup:

- .NET 10 SDK
- LLVM/Clang/LLDB 20
- Node/npm
- Codex CLI

The Dockerfile lives at [`Dockerfile.codex-linux`](../../Dockerfile.codex-linux).

## Build the image

```console
$ docker build -f Dockerfile.codex-linux -t flame-codex-linux .
```

## Run the container

Mount the repository into `/workspace`:

```console
$ docker run --rm -it \
    -v "$PWD:/workspace" \
    -w /workspace \
    flame-codex-linux
```

If you want to run Codex inside the container with your existing local configuration, also mount your Codex home directory:

```console
$ docker run --rm -it \
    -v "$PWD:/workspace" \
    -v "$HOME/.codex:/root/.codex" \
    -w /workspace \
    flame-codex-linux
```

## Run the LLVM backend tests

From inside the container:

```console
$ dotnet publish src/UnitTests/UnitTests.csproj -c Debug -r linux-x64 --self-contained false
$ dotnet src/UnitTests/bin/Debug/net10.0/linux-x64/publish/UnitTests.dll 5
```

Test set `5` runs the direct `Flame.Llvm` unit tests.

To run the Linux `IL2LLVM` tool tests:

```console
$ dotnet src/UnitTests/bin/Debug/net10.0/linux-x64/publish/UnitTests.dll 8 --clang-path clang
```

## Run Codex in the container

If the image was started with your Codex configuration mounted, you can launch Codex directly:

```console
$ codex
```

That lets you debug Linux-only issues from the container instead of waiting for CI.
