//! run:hello there

using System;

public struct StringWrapper
{
    public StringWrapper(string value)
    {
        this.value = value;
    }

    private string value;

    public override string ToString()
    {
        return value;
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        foreach (var item in args)
        {
            Print(new StringWrapper(item));
        }
    }

    private static void Print<T>(T value)
    {
        Console.WriteLine(value.ToString());
    }
}
