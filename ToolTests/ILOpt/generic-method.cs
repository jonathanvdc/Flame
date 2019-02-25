//! run:hello there

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            Print<string>(arg);
        }
    }

    public static T Print<T>(T value)
    {
        Console.WriteLine(value);
        return value;
    }
}
