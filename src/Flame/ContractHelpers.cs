using System;
using System.Runtime.Serialization;

namespace Flame
{
    /// <summary>
    /// A collection of functions that help enforce contracts.
    /// </summary>
    public static class ContractHelpers
    {
        [Serializable]
        private class AssertionException : Exception
        {
            public AssertionException() { }
            public AssertionException(string message) : base(message) { }
            public AssertionException(string message, Exception inner) : base(message, inner) { }
            protected AssertionException(
                SerializationInfo info,
                StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Asserts that a condition must always hold.
        /// </summary>
        /// <param name="condition">
        /// A condition that must be true.
        /// </param>
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new AssertionException("An assertion was unexpectedly false.");
            }
        }

        /// <summary>
        /// Asserts that a condition must always hold.
        /// </summary>
        /// <param name="condition">
        /// A condition that must be true.
        /// </param>
        /// <param name="message">
        /// The error message to print if the condition is false.
        /// </param>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new AssertionException(message);
            }
        }

        /// <summary>
        /// Checks that an integer value is positive, i.e., it is
        /// greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="valueName">The name of the value to check.</param>
        public static void CheckPositive(int value, string valueName)
        {
            Assert(
                value >= 0,
                "'" + valueName + "' should be positive. (value: " + value + ").");
        }
    }
}
