//! run:hello

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        var data = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            data[i] = args[i];
        }
        for (int i = 0; i < data.Length; i++)
        {
            Console.WriteLine(data[i]);
        }
    }
}
