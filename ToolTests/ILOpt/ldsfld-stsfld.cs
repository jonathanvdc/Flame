//! run:hello

using System;

public static class Program
{
    public static string globalValue;

    public static void Main(string[] args)
    {
        globalValue = args[0];
        Console.WriteLine(globalValue);
    }
}
