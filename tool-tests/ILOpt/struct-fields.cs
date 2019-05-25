//! run:hello

using System;

public struct StringContainer
{
    public string Contents;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var container = new StringContainer();
        container.Contents = args[0];
        Console.WriteLine(container.Contents);
    }
}
