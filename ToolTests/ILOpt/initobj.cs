using System;

public struct TestStruct
{
}

public static class Program
{
    public static void Main(string[] args)
    {
        var container = new TestStruct();
        Console.WriteLine(container);
    }
}
