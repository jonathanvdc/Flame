//! run

// A test based on a raytracing benchmark, adapted from https://github.com/zezba9000/RayTraceBenchmark .
// Licensed under the GPL-2.0 license.

//#define BIT64
//#define USE_OUT

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if BIT64
using Num = System.Double;
#else
using Num = System.Single;
#endif

namespace RayTraceBenchmark
{
    struct Vec3
    {
        public Num X, Y, Z;

        public static readonly Vec3 Zero = new Vec3();

        public Vec3(Num x, Num y, Num z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator +(Vec3 p1, Vec3 p2)
        {
            p1.X += p2.X;
            p1.Y += p2.Y;
            p1.Z += p2.Z;
            return p1;
        }

        public static Vec3 operator -(Vec3 p1, Vec3 p2)
        {
            p1.X -= p2.X;
            p1.Y -= p2.Y;
            p1.Z -= p2.Z;
            return p1;
        }

        public static Num Dot(Vec3 v1, Vec3 v2)
        {
            return (v1.X*v2.X) + (v1.Y*v2.Y) + (v1.Z*v2.Z);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }
    }

    struct Ray
    {
        public Vec3 Org;
        public Vec3 Dir;
    }

    class Sphere
    {
        public Vec3 Center;
        public Num Radius;
        public Vec3 Color;
        public Num Reflection;
        public Num Transparency;

        public Sphere(Vec3 c, Num r, Vec3 clr, Num refl = 0, Num trans = 0)
        {
            Center = c;
            Radius = r;
            Color = clr;
            Reflection = refl;
            Transparency = trans;
        }

        public static bool Intersect(Sphere sphere, Ray ray)
        {
            var l = sphere.Center - ray.Org;
            // Console.WriteLine(l);
            var a = Vec3.Dot(l, ray.Dir);
            Console.WriteLine(a);
            if (a < 0)              // opposite direction
            {
                Console.WriteLine("Opposite direction");
                return false;
            }

            var b2 = Vec3.Dot(l, l) - (a * a);
            Console.WriteLine(b2);
            var r2 = sphere.Radius * sphere.Radius;
            Console.WriteLine(r2);
            if (b2 > r2)            // perpendicular > r
            {
                Console.WriteLine("Perpendicular");
                return false;
            }

            return true;
        }
    }

    static class BenchmarkMain
    {
        static void Main(string[] args)
        {
            var sphere = new Sphere(new Vec3(-2.0f, -1.0f, -10.0f), 1, new Vec3(.1f, .1f, .1f), 0.1f, 0.8f);
            Ray r;
            r.Org = Vec3.Zero;
            r.Dir = new Vec3(0.3000566f, -0.3000566f, -0.9055009f);
            Console.WriteLine(Sphere.Intersect(sphere, r));

            var c = new Vec3(-2.0f, -1.0f, -10.0f);
            Console.WriteLine(Vec3.Dot(Vec3.Zero - c, r.Dir));
        }
    }
}
