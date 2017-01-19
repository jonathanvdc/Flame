# Build Status

Linux | Windows
----- | -------
[![Build Status](https://travis-ci.org/jonathanvdc/Flame.svg?branch=master)](https://travis-ci.org/jonathanvdc/Flame) | [![Build status](https://ci.appveyor.com/api/projects/status/yoe8u6g1p74570ir?svg=true)](https://ci.appveyor.com/project/jonathanvdc/flame)

# Flame

Flame is a collection of modular, open-source .NET libraries that can be leveraged to build a compiler.
It provides reusable components and a common compilation pipeline, which can be extended with additional passes, to suit the specific needs of source and target languages.

Flame specializes in compiling _managed code:_ object-oriented, garbage-collected, high-level programming languages, which are compiled ahead-of-time to some virtual machine's bytecode. Think C# and Java.

In a way, Flame is a compiler _construction kit_ for managed languages, much like LLVM is for native languages. Building your own compiler is easy: all you need is a front-end that generates valid Flame IR (that's short for _intermediate representation_, more on that later), and Flame will take care of the rest. Specifically, it will (among other things):

* Parse command-line options for you.
* Resolve library dependencies.
* Allow you to easily print expressive diagnostics, which can highlight source code.
* Optimize the IR that the front-end generates.
* Optionally link multiple assemblies together, at compile-time. As a bonus, unused types/methods/fields are discarded when the final executable is compiled.
* Perform codegen: a Flame back-end of your choice will generate an output assembly.

In short, it allows you to build and implement an awesome compiled programming language, instead of constantly having to worry about the details of the compilation process.

__Note__: Flame is still under development. It contains its fair share of bugs. Be sure to open an issue if you encounter one (especially when using 'stable' components).

## Getting and building Flame

[![NuGet](https://img.shields.io/nuget/v/Flame.Front.svg?maxAge=2592000)](https://www.nuget.org/packages/Flame.Front/)

If you want to use Flame as a library, then I recommend you get the latest stable [`Flame.Front` NuGet package](https://www.nuget.org/packages/Flame.Front/), and add that to your project.

If you for some reason can't, or don't want to, use NuGet, then you can get a pre-built version of `dsc` (and the Flame libraries it ships with) and optionally compile Flame yourself.

Note that Flame is partially bootstrapping: you'll need a working version of `dsc` to compile Flame. Fortunately, you can grab the latest (stable) version of `dsc` from the [releases page](https://github.com/jonathanvdc/Flame/releases). The download itself is `dsc.zip`. You can't miss it.

Once `dsc` has been downloaded and unzipped, you can use it to get Flame set up for you. There are two ways to do this:

### The easy way: copying the executables

You can convince `dsc` to copy all libraries (including Flame) that shipped with `dsc` to the working directory by running:

```bash
$ dsc -copy-rt
```

Create a console application, reference the libraries you require (you can probably lose Flame.DSharp, unless you need a D# front-end) in your project, and you're good to go. Note that you're using the latest _stable-ish_ version of the Flame libraries now. If you want the latest features, I recommend you take:

### The hard way: building Flame

Building Flame is actually not that hard. You'll need the following:
* D# compiler: `dsc` (the `dsc.zip` download). I recommend you put it in your path variable, or define it as an alias.
* C# compiler, like `csc` or `mcs`.
* F# compiler: `fsc` (if you want to compile the functional bindings for Flame).
* `msbuild` or `xbuild`.

Now run:

Linux:
```
$ ./BuildFlame.sh
```

Windows:
```cmd
$ BuildFlame.bat
$ msbuild /p:Configuration=Release Flame.Cecil\Flame.Cecil.sln
```

That's it. The Flame libraries you just compiled should be located in the `bin` subdirectories of the top-level Flame project directories.  

# Flame's architecture

## Front-ends, back-ends and drivers

There is no fixed input or output format for Flame: reading input and writing output is accomplished by individual front-ends and back-ends, respectively. The Flame libraries provide a common middle-end, as well as a number of back-ends. Flame currently has the following relatively stable back-ends:

* CLR (.NET)
* Flame's _intermediate representation_ (IR)

It also comes bundled with a number of _experimental_ back-ends, which may not support all source language features:

* C++ code (experimental)
* Python code (experimental)
* WebAssembly S-expressions (experimental)


This repository includes front-ends for Flame's _intermediate representation_ and the D# programming language. But Flame is extensible, so you can just write your own.

A _driver program_ is the actual compiler: it's a compact program that glues the Flame libraries and the front-end together. As an example, here's the source code for `dsc`'s main function: [Program.cs](https://github.com/jonathanvdc/Flame/blob/master/dsc/Program.cs).

## Intermediate representation

At the heart of Flame is the _intermediate representation_ (IR), which is a language-agnostic way of representing code. Front-ends generate this IR, back-ends consume it, and the middle-end optimizes it.

Flame IR can be stored both in-memory and on-disk. That's pretty neat, because it allows us to compile a project, save it as IR, and then link it with some other project, which can even be in another programming language.

# The D# programming language

D# is - roughly speaking - a dialect of C#. Implementation-wise, it's a general-purpose programming language that is implemented as a Flame front-end. `dsc` is the D# compiler.   

A number of Flame libraries are written in D#, but you don't need to know D# to use Flame.

# An overview of Flame libraries

## Flame
The core Flame library is mainly a reflection framework.
It provides common interfaces and functionality for assemblies, namespaces, types and type members.
Also, Flame contains some primitive types, whose functionality should have an equivalent on any platform.  
Flame is written in D#.

## Flame.Compiler
Flame.Compiler provides a common API for code generation, and is written in D#.  
A brief list of its functionality is as follows.

### Low-level code generation
This is achieved by the `ICodeGenerator` interface, which allows for the creation of `ICodeBlocks`: opaque, implementation-defined objects that represent a chunk of target-specific code.
Any `ICodeBlock` implementation must support yielding zero or one values, like a statement or expression.
These code blocks need not, however, restrict themselves to this model.
The `ICodeBlock` implementations for the .Net Framework IL, for example, conveniently model a stack, whereas a code block implementation for a register-based architecture may use registers instead.

### High-level expression-statement trees
The `IExpression` and `IStatement` interfaces are a programmer-friendly abstraction over the lower-level emit API.
Expressions can be queried for their type, and support compile-time evaluation.
Both statements and expressions have an "Optimize" method, which is intended to perform simple optimizations, like compile-time string concatenation.

### Common project interfaces
Flame.Compiler also includes some simple interfaces that define common project behavior to make project file format interoperation easier.

### Textual code generation
`CodeBuilder` and `CodeLine` ease the process of generating well-formatted textual code.

### Assembly creation
`IAssemblyBuilder`, `INamespaceBuilder`, `ITypeBuilder`, etc function as a portable interface for any back-end.

## Flame.Syntax
Flame.Syntax is a small project that makes writing front-ends and back-ends for various programming languages easier.  
Flame.Syntax is written in D#.

## Flame.DSharp
Flame.DSharp is the D# front-end which is used in dsc.  
It is written in D#.

## Flame.Cecil
Flame.Cecil facilitates reflecting upon and emitting .Net Framework assemblies.
dsc uses this library to reference and generate .Net assemblies.  
Flame.Cecil is written in C#.

## Flame.Cpp
Flame.Cpp is an experimental C++ back-end, which can be used by stating `-platform C++` when compiling with dsc.
Since Flame.Cpp cannot parse C++ itself, dsc "plugs" are used to allow the programmer to interact with the standard library from managed code.
Plugs for PlatformRT and PortableRT can be found in the "Examples" folder.  
Flame.Cpp is written in C#.

## Flame.Python
Flame.Python is an experimental Python back-end, accessible through `-platform Python` when compiling with dsc.  
Flame.Python is written in C#.

## Flame.Recompilation
Flame.Recompilation uses the assembly creation and decompilation interfaces of Flame.Compiler and the reflection facilities provided by Flame to "recompile" assemblies, namespaces, types and type members from one assembly to another.  
Flame.Recompilation is written in C#.

## dsc
dsc is a command-line utility that compiles D# code files and projects using Flame.DSharp and one of the various back-ends, such as Flame.Cecil, Flame.Python and Flame.Cpp.  
dsc is written in C#.
