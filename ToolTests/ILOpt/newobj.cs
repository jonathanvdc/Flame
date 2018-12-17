using System;

public class StringContainer
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
        var container = new StringContainer(args[0]);
        Console.WriteLine(container.Contents);
    }
}
