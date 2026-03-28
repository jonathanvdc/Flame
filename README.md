# Flame

Flame is a set of C# libraries and tools for reading, analyzing, optimizing, and writing managed code.

It is built around an SSA-based intermediate representation and includes:

- an IR designed for compiler analysis and transformation
- optimization passes such as inlining, scalar replacement, value numbering, control-flow simplification, and tail-recursion elimination
- analysis passes for dominators, liveness, value uses, predecessors, nullability, and more
- a CIL frontend and backend for translating between .NET IL and Flame IR
- command-line tools and examples built on top of the core libraries

Documentation:

- [Introduction](https://jonathanvdc.github.io/Flame/articles/intro.html)
- [API docs](http://jonathanvdc.github.io/Flame/api/)

## Repository layout

The main projects live under [`src/`](./src):

- `Flame`: core abstractions and type system
- `Flame.Compiler`: IR, analyses, and optimization passes
- `Flame.Clr`: CIL import/export and CLR-specific support
- `Flame.Ir`: IR serialization support
- `Flame.Llvm`: LLVM backend pieces
- `ILOpt`: CIL optimizer built on Flame
- `IL2LLVM`: IL-to-LLVM compilation tool
- `Examples/Brainfuck`: Brainfuck-to-CIL compiler example
- `Examples/TurboKernels`: GPU/kernel-oriented example
- `UnitTests`: unit tests and tool tests

Tool-oriented test inputs live under [`tool-tests/`](./tool-tests).

## Requirements

- .NET SDK 10.x

## Build

From the repository root:

```console
$ dotnet restore src/Flame.sln
$ dotnet build src/Flame.sln -c Debug
```

There is also a small `make` wrapper in [`src/Makefile`](./src/Makefile):

```console
$ make -C src debug
```

## Test

The main test entrypoint is the portable suite, which excludes LLVM-dependent tests:

```console
$ dotnet test src/UnitTests/UnitTests.csproj -c Debug --filter "TestCategory!=LLVM"
```

The `make` wrapper runs the same suite:

```console
$ make -C src test
```

The LLVM-specific tests are separate and require LLVM/Clang to be installed:

```console
$ make -C src test-llvm
```

To run the LLVM tests manually, specify the runtime identifier for your platform and set `CLANG_PATH` to the Clang executable:

```console
$ CLANG_PATH=clang dotnet test src/UnitTests/UnitTests.csproj -c Debug -r osx-arm64 --filter "TestCategory=LLVM"
```

Common runtime identifiers are `linux-x64`, `linux-arm64`, `osx-arm64`, `win-x64`, and `win-arm64`.

The current `tool-tests/IL2LLVM` samples use Linux `libc.so.6` imports. On non-Linux hosts, the LLVM tool tests skip those samples instead of failing; CI still exercises them on Linux.

## Tools

### `ilopt`

`ilopt` reads a managed assembly, optimizes it, and writes the optimized result back to disk.

Build it with the solution or directly:

```console
$ dotnet build src/ILOpt/ILOpt.csproj -c Debug
```

Run it with:

```console
$ dotnet src/ILOpt/bin/Debug/net10.0/ilopt.dll input.dll -o output.dll
```

The supported single-file samples used by the test suite live in [`tool-tests/ILOpt`](./tool-tests/ILOpt).

### `il2llvm`

`il2llvm` compiles managed assemblies to LLVM IR.

```console
$ dotnet publish src/IL2LLVM/IL2LLVM.csproj -c Debug -r osx-arm64 --self-contained false
$ dotnet src/IL2LLVM/bin/Debug/net10.0/osx-arm64/publish/il2llvm.dll input.dll -o output.ll
```

## Continuous integration

GitHub Actions is configured in [`.github/workflows/ci.yml`](./.github/workflows/ci.yml).

The CI workflow:

- restores `src/Flame.sln`
- builds the solution in `Debug`
- runs the portable test suite via `dotnet test --filter "TestCategory!=LLVM"`
- runs the LLVM backend test suite via `dotnet test -r linux-x64 --filter "TestCategory=LLVM"` with `CLANG_PATH` set to `clang-20`
