//! run

using System;
using System.Linq;

class IntroToLINQ
{
    static void Main()
    {
        // Define a sequence of numbers.
        int[] numbers = new int[7] { 0, 1, 2, 3, 4, 5, 6 };

        // Compute the square of those numbers
        var numQuery =
            from num in numbers
            select num * num;

        // Print the squared numbers.
        foreach (int num in numQuery.ToArray())
        {
            Console.Write("{0,1} ", num);
        }
        Console.WriteLine();
    }
}
