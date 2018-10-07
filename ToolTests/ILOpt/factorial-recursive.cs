//! run

using System;

public static class Program
{
    public static void Main()
    {
        for (int i = 1; i <= 10; i++)
        {
            Console.WriteLine(FactorialRecursive(i, 1));
        }
    }

    public static int FactorialRecursive(int value, int accumulator)
    {
        if (value > 1)
        {
            return FactorialRecursive(value - 1, value * accumulator);
        }
        else
        {
            return accumulator;
        }
    }
}
