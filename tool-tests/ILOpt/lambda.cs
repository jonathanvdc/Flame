//! run:hello

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        Apply(xs => Console.WriteLine(xs[0]), args);
    }

    private static void Apply<T>(Action<T> action, T value)
    {
        action(value);
    }
}
