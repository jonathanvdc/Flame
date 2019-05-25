using System.Diagnostics;

namespace Flame
{
    /// <summary>
    /// A collection of functions that help enforce contracts.
    /// </summary>
    public static class ContractHelpers
    {
        /// <summary>
        /// Asserts that a condition must always hold.
        /// </summary>
        /// <param name="condition">
        /// A condition that must be true.
        /// </param>
        public static void Assert(bool condition)
        {
            Debug.Assert(condition);
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
            Debug.Assert(condition, message);
        }

        /// <summary>
        /// Checks that an integer value is positive, i.e., it is
        /// greater than or equal to zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="valueName">The name of the value to check.</param>
        public static void CheckPositive(int value, string valueName)
        {
            Debug.Assert(
                value >= 0,
                "'" + valueName + "' should be positive. (value: " + value + ").");
        }
    }
}