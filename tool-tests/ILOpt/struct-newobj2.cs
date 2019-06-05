//! run:hello there

using System;

public struct StringWrapper
{
    public StringWrapper(string value)
    {
        this = default(StringWrapper);
        this.Value = value;
    }

    public string Value { get; private set; }
}

public static class Program
{
    public static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            Print(new StringWrapper(arg));
        }
    }

    public static void Print(StringWrapper wrapper)
    {
        Console.WriteLine(wrapper.Value);
    }
}
