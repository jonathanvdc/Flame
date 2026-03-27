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

The main test entrypoint is the portable suite:

```console
$ dotnet src/UnitTests/bin/Debug/net10.0/UnitTests.dll portable
```

The `make` wrapper runs the same suite:

```console
$ make -C src test
```

The LLVM-specific tests are separate:

```console
$ make -C src test-llvm
```

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
$ dotnet build src/IL2LLVM/IL2LLVM.csproj -c Debug
$ dotnet src/IL2LLVM/bin/Debug/net10.0/il2llvm.dll input.dll -o output.ll
```

## Continuous integration

GitHub Actions is configured in [`.github/workflows/ci.yml`](./.github/workflows/ci.yml).

The CI workflow:

- restores `src/Flame.sln`
- builds the solution in `Debug`
- runs the portable test suite
