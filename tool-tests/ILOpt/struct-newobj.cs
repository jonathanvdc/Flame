//! run:hello

using System;

public struct StringContainer
{
    public StringContainer(string contents)
    {
        this = default(StringContainer);
        this.Contents = contents;
    }

    public string Contents;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var containers = new StringContainer[1];
        containers[0] = new StringContainer(args[0]);
        Console.WriteLine(containers[0].Contents);
    }
}
