//! run:hello there

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        foreach (var item in args)
        {
            Print(item);
        }
    }

    private static void Print<T>(T value)
    {
        Console.WriteLine(value.ToString());
    }
}
