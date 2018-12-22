//! run:hello there

using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
        var items = new List<string>();
        items.AddRange(args);
        for (int i = 0; i < items.Count; i++)
        {
            Console.WriteLine(items[i]);
        }
    }
}
