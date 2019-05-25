//! run:hello

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        args[0] = "howdy";
        Console.WriteLine(args[0]);
    }
}
