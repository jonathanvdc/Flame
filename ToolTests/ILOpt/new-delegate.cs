//! run:hello

using System;
using System.Collections.Generic;

public static class Program
{
    private static void PrintFirstElement(this string firstArg, string[] elements)
    {
        Console.WriteLine(elements[0]);
    }

    public static void Main(string[] args)
    {
        Action<string, string[]> deleg2 = PrintFirstElement;
        Action<string[]> deleg = ((string)null).PrintFirstElement;
        deleg(args);
        deleg2(null, args);
    }
}
