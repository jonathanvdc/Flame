//! run

using System;
using System.Collections.Generic;
using System.Linq;

public static class Program
{
    public static void Main()
    {
        foreach (var item in Fibonacci().Take(20))
        {
            Console.WriteLine(item);
        }
    }

    public static IEnumerable<int> Fibonacci()
    {
        int penultimate = 1;
        int ultimate = 1;
        yield return penultimate;
        while (true)
        {
            var next = penultimate + ultimate;
            yield return ultimate;
            penultimate = ultimate;
            ultimate = next;
        }
    }
}
