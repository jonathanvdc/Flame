using System;
using System.Collections.Immutable;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing arithmetic intrinsics.
    /// </summary>
    public static class ArithmeticIntrinsics
    {
        private const string arithmeticIntrinsicPrefix = "arith";

        /// <summary>
        /// Tries to parse an intrinsic name as an arithmetic
        /// intrinsic name.
        /// </summary>
        /// <param name="intrinsicName">
        /// The intrinsic name to parse.
        /// </param>
        /// <param name="operatorName">
        /// The name of the operator specified by the intrinsic,
        /// if the intrinsic name is an arithmetic intrinsic name.
        /// </param>
        /// <returns>
        /// <c>true</c> if the intrinsic name is an arithmetic
        /// intrinsic name; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryParseArithmeticIntrinsicName(
            string intrinsicName,
            out string operatorName)
        {
            // All arithmetic intrinsics have the following name format:
            //
            //    arith.<op>
            //
            var splitName = intrinsicName.Split(new char[] { '.' }, 2);
            if (splitName.Length != 2 || splitName[0] != arithmeticIntrinsicPrefix)
            {
                operatorName = null;
                return false;
            }
            else
            {
                operatorName = splitName[1];
                return true;
            }
        }

        /// <summary>
        /// Parses an intrinsic name as an arithmetic intrinsic name,
        /// assuming that the intrinsic name is an arithmetic intrinsic
        /// name. Returns the name of the operator wrapped by the
        /// arithmetic intrinsic name.
        /// </summary>
        /// <param name="intrinsicName">
        /// The arithmetic intrinsic name to parse.
        /// </param>
        /// <returns>
        /// The operator name wrapped by the arithmetic intrinsic name.
        /// </returns>
        public static string ParseArithmeticIntrinsicName(
            string intrinsicName)
        {
            string result;
            if (TryParseArithmeticIntrinsicName(intrinsicName, out result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException(
                    $"Name '{intrinsicName}' is not an arithmetic intrinsic name.");
            }
        }

        /// <summary>
        /// Tests if an intrinsic name is an arithmetic intrinsic name.
        /// </summary>
        /// <param name="intrinsicName">
        /// The intrinsic name to examine.
        /// </param>
        /// <returns>
        /// <c>true</c> if the intrinsic name is an arithmetic intrinsic name;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool IsArithmeticIntrinsicName(string intrinsicName)
        {
            string opName;
            return TryParseArithmeticIntrinsicName(intrinsicName, out opName);
        }

        /// <summary>
        /// Creates an arithmetic intrinsic name from an
        /// arithmetic operator name.
        /// </summary>
        /// <param name="operatorName">
        /// The operator name to wrap in an arithmetic intrinsic name.
        /// </param>
        /// <returns>
        /// An arithmetic intrinsic name.
        /// </returns>
        public static string GetArithmeticIntrinsicName(string operatorName)
        {
            return arithmeticIntrinsicPrefix + "." + operatorName;
        }

        /// <summary>
        /// A collection of names for arithmetic operations.
        /// </summary>
        public static class Operators
        {
            /// <summary>
            /// The addition binary operator.
            /// </summary>
            public const string Add = "add";

            /// <summary>
            /// The subtraction binary operator.
            /// </summary>
            public const string Subtract = "sub";

            /// <summary>
            /// The multiplication binary operator.
            /// </summary>
            public const string Multiply = "mul";

            /// <summary>
            /// The division binary operator.
            /// </summary>
            public const string Divide = "div";

            /// <summary>
            /// The remainder binary operator.
            /// </summary>
            public const string Remainder = "rem";

            /// <summary>
            /// The is-greater-than binary operator.
            /// </summary>
            public const string IsGreaterThan = "gt";

            /// <summary>
            /// The is-less-than binary operator.
            /// </summary>
            public const string IsLessThan = "lt";

            /// <summary>
            /// The is-equal-to binary operator.
            /// </summary>
            public const string IsEqualTo = "eq";

            /// <summary>
            /// The is-not-equal-to binary operator.
            /// </summary>
            public const string IsNotEqualTo = "neq";

            /// <summary>
            /// The is-greater-than-or-equal-to binary operator.
            /// </summary>
            public const string IsGreaterThanOrEqualTo = "gte";

            /// <summary>
            /// The is-less-than-or-equal-to binary operator.
            /// </summary>
            public const string IsLessThanOrEqualTo = "lte";

            /// <summary>
            /// The bitwise not unary operator.
            /// </summary>
            public const string Not = "not";

            /// <summary>
            /// The bitwise and binary operator.
            /// </summary>
            public const string And = "and";

            /// <summary>
            /// The bitwise or binary operator.
            /// </summary>
            public const string Or = "or";

            /// <summary>
            /// The bitwise exclusive or binary operator.
            /// </summary>
            public const string Xor = "xor";

            /// <summary>
            /// An immutable array containing all standard arithmetic
            /// intrinsic operator names.
            /// </summary>
            public static readonly ImmutableArray<string> All =
                ImmutableArray.Create(
                    new[]
                    {
                        Add,
                        Subtract,
                        Multiply,
                        Divide,
                        Remainder,
                        IsGreaterThan,
                        IsLessThan,
                        IsEqualTo,
                        IsNotEqualTo,
                        IsGreaterThanOrEqualTo,
                        IsLessThanOrEqualTo,
                        Not,
                        And,
                        Or,
                        Xor
                    });
        }
    }
}
