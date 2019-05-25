//! run

// This test case is based on Georgiy Mogelashvili (@glamcoder)'s binary heap
// benchmark, licensed under the MIT license and hosted at
// https://github.com/MikeMirzayanov/binary-heap-benchmark

using System;

namespace HeapBenchmark
{
    class Heap
    {
        private const int N = 10;
        private static int[] h = new int[N];

        private static void PushDown(int pos, int n)
        {
            while (2*pos + 1 < n)
            {
                int j = 2*pos + 1;
                if (j + 1 < n && h[j + 1] > h[j])
                    j++;
                if (h[pos] >= h[j])
                    break;
                int t = h[pos];
                h[pos] = h[j];
                h[j] = t;
                pos = j;
            }
        }

        public static void Main(String[] args)
        {
            for (int i = 0; i < N; i++)
                h[i] = i;

            for (int i = N/2; i >= 0; i--)
                PushDown(i, N);

            int n = N;
            while (n > 1)
            {
                int t = h[0];
                h[0] = h[n - 1];
                h[n - 1] = t;
                n--;
                PushDown(0, n);
            }

            for (int i = 0; i < N; i++)
            {
                Console.WriteLine(h[i]);
            }
        }
    }
}
