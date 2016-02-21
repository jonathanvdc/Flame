# Flame [![Build Status](https://travis-ci.org/jonathanvdc/Flame.svg?branch=master)](https://travis-ci.org/jonathanvdc/Flame)
Flame is a collection of projects that form a compiler framework for managed code.
It also includes a compiler for the homegrown D# programming language.
Flame's eventual goal is to provide a framework that can be used to compile code in a programming language to relatively efficient machine code or translate code to idiomatic code in another programming language.
It also intends to be a useful library for expression/statement manipulation, reflection and code generation.  
A summary of some Flame projects:

## Flame
The core Flame library is mainly a reflection framework.
It provides common interfaces and functionality for assemblies, namespaces, types and type members.
Also, Flame contains some primitive types, whose functionality should have an equivalent on any platform.  
Flame is written in D#.

## Flame.Compiler
Flame.Compiler provides a common API for code generation, and is written in D#.  
A brief list of its functionality is as follows.

### Low-level code generation
This is achieved by the ICodeGenerator interface, which allows for the creation of ICodeBlocks, which are rather opaque, implementation-defined objects.
Any ICodeBlock implementation must support yielding zero or one values, like a statement or expression.
These code blocks need not, however, restrict themselves to this model. 
The ICodeBlock implementations for the .Net Framework IL, for example, conveniently model a stack, whereas a code block implementation for a register-based architecture may use registers instead.

### High-level expression-statement trees
The IExpression and IStatement interfaces are a programmer-friendly abstraction over the lower-level emit API.
Expressions can be queried for their type, and support compile-time evaluation.
Both statements and expressions have an "Optimize" method, which is intended to perform simple optimizations, like compile-time string concatenation.

### Common project interfaces
Flame.Compiler also includes some simple interfaces that define common project behavior to make project file format interoperation easier.

### Textual code generation
CodeBuilder and CodeLine ease the process of generating well-formatted textual code.

### Assembly creation
IAssemblyBuilder, INamespaceBuilder, ITypeBuilder, etc function as a portable interface for any back-end.

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
