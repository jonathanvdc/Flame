//! run

// This is a regression test for a bug where the optimizer would inappropriately
// reorder array element loads and stores.
//
// It is reduced from Georgiy Mogelashvili (@glamcoder)'s binary heap
// benchmark, licensed under the MIT license and hosted at
// https://github.com/MikeMirzayanov/binary-heap-benchmark

using System;
namespace a
{
    class b
    {
        const int c = 10;
        static int[] h = new int[c];
        static void d(int e, int f)
        {
            while (1 < f)
            {
                int g = 1;
                if (h[e] >= g)
                    break;

                // Bug: load from h[e] gets reordered with store to h[e].
                int j = h[e];
                h[e] = h[g];
                h[g] = j;
                Console.WriteLine(j);
            }
        }
        static void Main()
        {
            for (int i = 0; i < c; i++)
                h[i] = i;
            for (int i = 2; i >= 0; i--)
                d(i, c);
        }
    }
}
