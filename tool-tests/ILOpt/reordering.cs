//! run

using System;

public struct Point
{
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
    public double X { get; private set; }
    public double Y { get; private set; }
}

public static class Program
{
    private static void AddByOut(ref Point a, ref Point b, out Point c)
    {
        c = new Point(a.X + b.Y, a.Y + b.X);
    }

    public static void Main()
    {
        var vec = new Point(42, 10);
        Run(ref vec);
    }

    private static void Run(ref Point vec)
    {
        Console.WriteLine(vec.X);
        Console.WriteLine(vec.Y);
        AddByOut(ref vec, ref vec, out vec);
        Console.WriteLine(vec.X);
        Console.WriteLine(vec.Y);
    }
}
