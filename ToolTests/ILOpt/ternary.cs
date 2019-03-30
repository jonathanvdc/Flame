//! run:hello there

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("{0}", args.Length == 0 ? "" : args[0]);
        Console.WriteLine("{0}", args.Length == 1 ? "" : args[0]);
    }
}
