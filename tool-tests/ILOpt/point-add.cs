//! run

// Based on the benchmark program from ".NET Struct Performance"
// by Christopher Nahr.
// Article and original source code at:
// http://www.kynosarges.org/StructPerformance.html
//
// We're not using it as an actual benchmark: this program's AddByVal
// function tests the stack machine instruction stream builder.

using System;

public struct Point
{
    public Point(double x, double y)
    {
        this = default(Point);
        X = x; Y = y;
    }
    public double X { get; private set; }
    public double Y { get; private set; }

    public override string ToString()
    {
        return string.Format("({0}, {1})", X, Y);
    }
}

public static class CSharpTest
{
    private static Point AddByVal(Point a, Point b)
    {
        return new Point(a.X + b.Y, a.Y + b.X);
    }

    public static void Main()
    {
        Point a = new Point(1, 1), b = new Point(1, 1);
        Console.WriteLine(AddByVal(a, b));
    }
}
