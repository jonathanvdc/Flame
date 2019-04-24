//! run

using System;

public struct Vector2
{
    public int x;
    public int y;
}

public static class Program
{
    public static void Main()
    {
        var vec = Accumulate();
        Console.WriteLine(vec.x);
        Console.WriteLine(vec.y);
    }

    private static Vector2 Accumulate()
    {
        // Create a vector and do some things with it that are
        // amenable to scalar replacement.
        var accumulator = new Vector2();
        accumulator.x = 0;
        accumulator.y = 0;
        for (int i = 0; i < 10; i++)
        {
            accumulator.x++;
            accumulator.y++;
        }

        // Make the vector escape, so it isn't eligible for full
        // scalar replacement.
        Escape(ref accumulator);

        // Return the vector.
        return accumulator;
    }

    private static void Escape(ref Vector2 value)
    { }
}