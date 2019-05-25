//! run:hello

using System;

public struct StringContainer
{
    public StringContainer(string contents)
    {
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
        ref var containerRef = ref containers[0];
        Console.WriteLine(containerRef.Contents);
    }
}
