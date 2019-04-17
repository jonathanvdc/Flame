# Building a Brainfuck compiler with Flame

## Introduction

This tutorial aims to be a gentle introduction to using [Flame](https://github.com/jonathanvdc/Flame). Flame is a set of reusable libraries that can be leveraged to build a compiler. It's especially good at generating code for managed languages like C\#, but it's also perfectly capable of compiling, say, Brainfuck. Which is exactly what we're going to do in this tutorial: we're going to build an AOT compiler for Brainfuck, and we'll produce executables that can be run by implementations of the .NET common language runtime \(CLR\), e.g., [Mono](http://www.mono-project.com/) or the [.NET framework](https://www.microsoft.com/net/). I'm assuming that you're familiar with C\# development and a C\# IDE of your choice. You should also know how to use command-line programs, because that's what we'll be building in this tutorial.

If you don't know what Brainfuck is, then I recommend that you take a look at [its Wikipedia page](https://en.wikipedia.org/wiki/Brainfuck). The important bit is that it's an extremely minimal programming language, so it'll be super easy to implement.

This tutorial will contain a fair amount of code but will focus mostly on a few key aspects of the Brainfuck compiler&mdash;the bits that'll hopefully help you grok Flame. If you want the full source code, then you'll find that in the [Flame GitHub repo](https://github.com/jonathanvdc/Flame/tree/csharp-rewrite/Examples/Brainfuck).

If you have any questions or comments, feel free to open an issue&mdash;or maybe even a pull request!&mdash;on [GitHub](https://github.com/jonathanvdc/Flame/). Your feedback and/or contributions are most welcome.

Anyway, let's get started.

## Getting started

TODO: actual tutorial.
