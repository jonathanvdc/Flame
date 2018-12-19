using System;

public struct IntContainer
{
    public int Contents;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var container = new IntContainer();
        container.Contents = 42;
        Print(ref container.Contents);
    }

    private static void Print(ref int number)
    {
        Console.WriteLine(number);
    }
}
