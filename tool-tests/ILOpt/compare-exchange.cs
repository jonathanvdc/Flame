//! run

using System;
using System.Threading;

public class Int32Box
{
    public Int32Box(int value)
    {
        this.Value = value;
    }

    public int Value;

    public override string ToString()
    {
        return Value.ToString();
    }
}

public static class Program
{
    private static Int32Box x = new Int32Box(10);

    public static void Main()
    {
        Console.WriteLine(Interlocked.CompareExchange(ref x, new Int32Box(20), x));
        Console.WriteLine(x);
    }
}
