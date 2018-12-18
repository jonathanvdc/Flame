# Flame

This is a C# rewrite of Flame, a set of compiler libraries for managed languages. It is currently a work in progress.

## Subprojects

At its core, Flame is a set of libraries designed to support tools that read, analyze, optimize and write managed languages. Additionally, Flame includes a number of projects that use Flame in a fairly straightforward way.

### `ilopt`

`ilopt` is a command-line tool that reads CIL assemblies, optimizes them and writes the optimized version back to disk somewhere. `ilopt` can already optimize some assemblies, but it only supports a subset of CIL at the moment. Most work being done is focused on teaching `ilopt` to round-trip all CIL opcodes.

## Build instructions

Flame is a C# project that targets .NET 4.5 implementations, like Mono and the .NET framework. .NET Core is not supported yet because Flame relies on NuGet packages that don't yet support .NET Core.

Additionally, Flame uses [EC#](http://ecsharp.net/) macros to convert Flame's IR rewrite rule DSL to C# code.

### Linux, Mac OS X

Building Flame is easy if you're on Linux or Mac OS X. Just spell
```console
$ make nuget
$ make
```

That's it. The above will grab NuGet dependencies, compile EC# macros down to regular C# and build the project.

To run the unit tests, type
```console
$ make test
```

### Windows

Building Flame is somewhat more challenging on Windows. If at all possible, use a GNU Make implementation to run the Makefile, same as for Linux and Mac OS X.

Otherwise, you will need to do the following:

  1. Restore NuGet packages (`nuget restore`).
  2. Build the macros. (`msbuild /p:Configuration=Release FlameMacros/FlameMacros.csproj`).
  3. Compile EC# macros downn to regular C# (`make dsl` in the Makefile, otherwise `FlameMacros/bin/Release/LeMP.exe --macros FlameMacros/bin/Release/FlameMacros.dll --outext=.out.cs file.ecs` for all `.ecs` files).
  4. Build Flame itself (`msbuild /p:Configuration=Release Flame.sln`).

Run the unit tests by spelling
```console
UnitTests\bin\Release\UnitTests.exe 123456
```

Windows workflow enhancements welcome!
