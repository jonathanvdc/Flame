//! run

using System;

public static class Program
{
    private static int one = 1;
    private static int lots = 2048;

    public static void Main()
    {
        Console.WriteLine(one << 10);
        Console.WriteLine(lots >> 10);
        Console.WriteLine((uint)lots >> 10);
        Console.WriteLine((uint)one << 10);
    }
}
