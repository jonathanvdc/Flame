//! run

// A test reduced from a raytracing benchmark at https://github.com/zezba9000/RayTraceBenchmark .
// Licensed under the GPL-2.0 license.
//
// This is a regression test for a bug in the stack machine instruction stream builder that
// caused the following stream of instructions to be created:
//
//     IL_0087:  ldloc.1
//     IL_0088:  ldloca.s 1
//     IL_008a:  initobj [mscorlib]System.Single
//
// This is, a value was loaded from a virtual register before that virtual register was initialized.
// The application was just reading out garbage, essentially.
//
// This bug is hard to diagnose because CIL zero-initializes variables. So the change in semantics
// caused by the codegen bug only appears when a virtual register is reused.

using System;
using a = System.Single;
namespace b
{
    struct d
    {
        public a e, f, g;
        public static d h;
        public static d operator -(d aa, d j)
        {
            aa.e -= j.e;
            return aa;
        }
        public static d operator -(d aa)
        {
            aa.e = -aa.e;
            return aa;
        }
        public static d operator *(d aa, a j)
        {
            aa.e = j;
            return aa;
        }
        public static d operator /(d aa, a j)
        { return aa; }
        public static a ab(d m, d n)
        { return m.e * n.e + n.g; }
        public static d t(d u)
        { return u / (a)Math.Sqrt(0 + u.g); }
    }
    struct p
    {
        public d w;
        public d q;
    }
    class s
    {
        d z;
        public s(d c, a r, d b)
        { }
        public static d ac(s ad, d ae)
        { return d.t(ae - ad.z); }
    }
    class af
    {
        public d ag;
        public af(d ah, d aj)
        { }
    }
    class ak
    {
        s[] al;
        af[] am;
        const int an = 100;
        const int v = 100;
        static d ao(p ai, ak ap, int aq)
        {
            s ar = ap.al[0];
            var point_of_hit = ai.q;
            var normal = s.ac(ar, point_of_hit);
            if (d.ab(normal, point_of_hit) > 0)
            {
                normal = -normal;
            }
            Console.WriteLine(d.ab(normal, point_of_hit));
            return new d();
        }
        static byte[] y(ak ap, byte[] pixels)
        {
            for (int k = 0; k != an; ++k)
            {
                d dir;
                dir.e = dir.f = dir.g = 1.0f;
                p r;
                r.w = r.q = dir;
                var pixel = ao(r, ap, 0);
                int i = k;
                pixels[i] = (byte)Math.Min(pixel.e, 5);
            }
            return pixels;
        }
        static void Main()
        {
            Start();
        }
        static byte[] Start()
        {
            var ap = new ak();
            ap.al = new[] { new s(new d(), 1, new d()) };
            ap.am = new[] { new af(new d(), new d()) };
            int pixelsLength = v;
            byte[] pixels = new byte[pixelsLength];
            y(ap, pixels);
            return pixels;
        }
    }
}
