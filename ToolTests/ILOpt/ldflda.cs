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
        Print(ref container.Contents);
    }

    private static void Print(ref string text)
    {
        Console.WriteLine(text);
    }
}
