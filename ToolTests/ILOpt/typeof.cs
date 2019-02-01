//! run

using System;

public static class Program
{
    public static void Main()
    {
        int i = 10;
        Console.WriteLine(typeof(int).ToString());
        Console.WriteLine(typeof(int) == i.GetType());
    }
}
