//! run:hello there

using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
        PrintAll(args);
    }

    private static void PrintAll(IEnumerable<string> args)
    {
        foreach (var arg in args)
        {
            Console.WriteLine(arg);
        }
    }
}
