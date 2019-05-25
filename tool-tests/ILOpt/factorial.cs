//! run

using System;

public static class Program
{
    public static void Main()
    {
        for (int i = 1; i <= 10; i++)
        {
            Console.WriteLine(Factorial(i));
        }
    }

    public static int Factorial(int value)
    {
        int result = 1;
        while (value > 1)
        {
            result *= value;
            value--;
        }
        return result;
    }
}
