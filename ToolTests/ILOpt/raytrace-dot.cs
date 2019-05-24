//! run

// This test was reduced from the raytrace benchmark (https://github.com/zezba9000/RayTraceBenchmark),
// licensed under the GPL-2.0 license.
// It is designed to test the stack machine instruction stream builder.

using System;
struct Vec2
{
    float X, Y;
    public static Vec2 Zero;
    public static float Dot(Vec2 v1, Vec2 v2)
    {
        return (v1.X * v2.X) + v2.Y;
    }
}
public static class Program
{
    static void Main()
    {
        Console.WriteLine(Vec2.Dot(Vec2.Zero, Vec2.Zero));
    }
}
