using System;
using System.Threading;
using Turbo;

namespace TurboKernels
{
    public static class Program
    {
        public static void Kernel()
        {

        }

        public static void Main()
        {
            Parallel.ForAsync(20, Kernel).Wait();

            // Atomically increment a value twenty times on the device.
            int x = 10;
            Parallel.ForAsync(20, () => { Interlocked.Increment(ref x); }).Wait();
            // Now carefully increment the value exactly once.
            Parallel.ForAsync(20, () => { Interlocked.CompareExchange(ref x, 31, 30); }).Wait();
            Console.WriteLine(x);

            // Use the unique thread ID to compute the sum of all integers
            // from one through one hundred.
            int y = 0;
            Parallel.ForAsync(100, threadId => { Interlocked.Add(ref y, threadId + 1); }).Wait();
            Console.WriteLine(y);
        }
    }
}
