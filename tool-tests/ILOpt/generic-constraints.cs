//! run:hello there

using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
    public static void PrintUnique<T>(IEnumerable<T> values)
        where T : IEquatable<T>
    {
        var list = new List<T>();
        foreach (var item in values)
        {
            bool unique = true;
            foreach (var elem in list)
            {
                if (item.Equals(elem))
                {
                    unique = false;
                    break;
                }
            }

            if (unique)
            {
                list.Add(item);
                Console.WriteLine(item);
            }
        }
    }

    public static void PrintUnique<T>(params T[] values)
        where T : IEquatable<T>
    {
        PrintUnique((IEnumerable<T>)values);
    }

    public static void Main(string[] args)
    {
        PrintUnique(1, 2, 3, 10, 2, 42, 3, 1, 7);
    }
}
