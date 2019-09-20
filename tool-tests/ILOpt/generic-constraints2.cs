//! run:hello there

using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
    public static void PrintUnique<T>(T first, T second)
        where T : IEquatable<T>
    {
        Console.WriteLine(first);
        if (!first.Equals(second))
        {
            Console.WriteLine(second);
        }
    }

    public static void Main(string[] args)
    {
        PrintUnique(1, 2);
        PrintUnique(3, 3);
    }
}
