using System;
using System.Collections.Generic;

namespace Flame.Llvm
{
    // TODO: move this file to Flame.Collections?
    // TODO: use bignums.

    /// <summary>
    /// Generates prime numbers.
    /// </summary>
    internal sealed class PrimeNumberGenerator
    {
        /// <summary>
        /// Creates a new prime number generator.
        /// </summary>
        public PrimeNumberGenerator()
        {
            primes = new List<ulong>();
            primes.Add(2);
            nextPrime = 3;
        }

        private List<ulong> primes;
        private ulong nextPrime;

        /// <summary>
        /// Generates and returns the next prime number.
        /// </summary>
        /// <returns>A prime number.</returns>
        public ulong Next()
        {
            // Based on David Johnstone's answer to this StackOverflow question:
            // https://stackoverflow.com/questions/1042902/most-elegant-way-to-generate-prime-numbers

            bool stillLooking = true;
            while (stillLooking)
            {
                ulong sqrt = (ulong)Math.Sqrt(nextPrime);
                bool isPrime = true;
                for (int i = 0; primes[i] <= sqrt; i++)
                {
                    if (nextPrime % primes[i] == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime)
                {
                    primes.Add(nextPrime);
                    stillLooking = false;
                }
                nextPrime += 2;
            }
            return primes[primes.Count - 2];
        }
    }
}
