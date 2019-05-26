//! run

using System;

public static class Program
{
    public static volatile int value;

    public static void Main()
    {
        value = 42;
        Console.WriteLine(value);
    }
}
