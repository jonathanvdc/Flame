//! run:hello there:hello

using System;

public struct Container
{
    public string[] value;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var arr = new Container[1];
        arr[0] = default(Container);
        arr[0].value = args;
        Console.WriteLine(arr[0].value[0]);
    }
}
