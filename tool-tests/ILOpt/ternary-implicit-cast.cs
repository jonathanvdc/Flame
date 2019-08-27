//! run:hello there

using System;

public static class Program
{
    private static readonly object defaultResponse = "bleep bloop";

    public static void Main(string[] args)
    {
        bool useSpecialResponse = args.Length == 2 && args[0] == "hello" && args[1] == "there";
        // The ternary below creates two paths. One pushes a string literal on the stack
        // (type `string`) and another loads a field of type `object`. Both paths then branch
        // back to a common call to `Console.WriteLine`. The test ensures that the CIL analyzer
        // can deal with these divergent types on the stack.
        Console.WriteLine(
            useSpecialResponse
                ? "General Kenobi"
                : defaultResponse);
    }
}
