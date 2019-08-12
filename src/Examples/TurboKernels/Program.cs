using System;
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

            // TODO: implement logic that copies changes to call target
            // (closure over 'x' in this case) back from the device.
            int x = 10;
            Parallel.ForAsync(20, () => { x++; }).Wait();
        }
    }
}
