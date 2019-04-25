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
    private static Point AddByVal(Point a, Point b)
    {
        return new Point(a.X + b.Y, a.Y + b.X);
    }

    public static void Main()
    {
        var vec = AddByVal(new Point(42, 10), new Point(25, 50));
        Console.WriteLine(vec.X);
        Console.WriteLine(vec.Y);
    }
}
