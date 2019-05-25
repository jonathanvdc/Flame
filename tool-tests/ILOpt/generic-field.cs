//! run:hello there

using System;
using System.Collections.Generic;

public class Box<T>
{
    public T value;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var box = new Box<string>();
        box.value = args[0];
        Console.WriteLine(box.value);
    }
}
