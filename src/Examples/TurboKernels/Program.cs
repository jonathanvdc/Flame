using Turbo;

namespace TurboKernels
{
    public static class Program
    {
        public static void Kernel()
        {

        }

        public static void Kernel2(int arg)
        {

        }

        public static void Main()
        {
            Parallel.ForAsync(20, Kernel).Wait();
            Parallel.ForAsync(20, Kernel2, 42).Wait();
        }
    }
}
