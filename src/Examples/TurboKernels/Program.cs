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
            Console.WriteLine(x);
        }
    }
}
