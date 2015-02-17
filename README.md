# Flame
Flame is a collection of projects that form a compiler framework for managed code.
It also includes a compiler for the homegrown D# programming language.  
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
The ICodeBlock implementations for the .Net Framework IL, for example, conventiently model a stack, whereas a code block implementation for a register-based architecture may use registers instead.

### High-level expression-statement trees
The IExpression and IStatement interfaces are a programmer-friendly abstraction over the lower-level emit API.
Expressions can be queried for their type, and support compile-time evaluation.
Both statements and expressions have an "Optimize" method, which is intended to perform simple optimizations, like compile-time string concatenation.

### Common project interfaces
Flame.Compiler also includes some simple interfaces that define common project behavior to make project file format interoperation easier.

### Textual code generation
CodeBuilder and CodeLine ease the process generating well-formatted textual code.

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

## dsc
dsc is a command-line utility that compiles D# code files and projects using Flame.DSharp and one of the various back-ends, such as Flame.Cecil, Flame.Python and Flame.Cpp.  
dsc is written in C#.
