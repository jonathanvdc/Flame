//! run

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("One");
        }
        finally
        {
            Console.WriteLine("Two");
        }
    }
}
