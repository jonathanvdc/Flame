using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Supports creating, recognizing and parsing arithmetic intrinsics.
    /// </summary>
    public static class ArithmeticIntrinsics
    {
        /// <summary>
        /// The namespace for arithmetic intrinsics.
        /// </summary>
        public static readonly IntrinsicNamespace Namespace =
            new IntrinsicNamespace("arith");

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
            return Namespace.TryParseIntrinsicName(intrinsicName, out operatorName);
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
            return Namespace.ParseIntrinsicName(intrinsicName);
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
            return Namespace.IsIntrinsicName(intrinsicName);
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
            return Namespace.GetIntrinsicName(operatorName);
        }

        /// <summary>
        /// Creates an arithmetic intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the arithmetic intrinsic.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic to create.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the values the intrinsic takes as arguments.
        /// </param>
        /// <returns>
        /// An arithmetic intrinsic prototype.
        /// </returns>
        public static IntrinsicPrototype CreatePrototype(
            string operatorName,
            IType resultType,
            params IType[] parameterTypes)
        {
            // TODO: exception specification?
            return CreatePrototype(
                operatorName,
                resultType,
                (IReadOnlyList<IType>)parameterTypes);
        }

        /// <summary>
        /// Creates an arithmetic intrinsic prototype.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator represented by the arithmetic intrinsic.
        /// </param>
        /// <param name="resultType">
        /// The type of value produced by the intrinsic to create.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the values the intrinsic takes as arguments.
        /// </param>
        /// <returns>
        /// An arithmetic intrinsic prototype.
        /// </returns>
        public static IntrinsicPrototype CreatePrototype(
            string operatorName,
            IType resultType,
            IReadOnlyList<IType> parameterTypes)
        {
            // TODO: exception specification?
            return IntrinsicPrototype.Create(
                GetArithmeticIntrinsicName(operatorName),
                resultType,
                parameterTypes);
        }

        /// <summary>
        /// Tries to evaluate an application of a standard arithmetic operator.
        /// </summary>
        /// <param name="operatorName">
        /// The name of the operator to evaluate.
        /// </param>
        /// <param name="prototype">
        /// The full intrinsic prototype for the arithmetic operator.
        /// </param>
        /// <param name="arguments">
        /// The operator application's arguments.
        /// </param>
        /// <param name="result">
        /// The operator application's result, if it can be computed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the operator application can be evaluated; otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool TryEvaluate(
            string operatorName,
            IntrinsicPrototype prototype,
            IReadOnlyList<Constant> arguments,
            out Constant result)
        {
            if (arguments.Count == 2)
            {
                var lhs = arguments[0];
                var rhs = arguments[1];
                if (prototype.ResultType.IsIntegerType()
                    && lhs is IntegerConstant
                    && rhs is IntegerConstant)
                {
                    var resultSpec = prototype.ResultType.GetIntegerSpecOrNull();
                    var intLhs = (IntegerConstant)lhs;
                    var intRhs = (IntegerConstant)rhs;
                    var maxIntSpec = UnionIntSpecs(
                        resultSpec,
                        intLhs.Spec,
                        intRhs.Spec);

                    Func<IntegerConstant, IntegerConstant, IntegerConstant> impl;
                    if (intBinaryOps.TryGetValue(operatorName, out impl))
                    {
                        result = impl(
                            intLhs.Cast(maxIntSpec),
                            intRhs.Cast(maxIntSpec)).Cast(resultSpec);
                        return true;
                    }
                }
                else if (prototype.ResultType == prototype.ParameterTypes[0]
                    && lhs is Float64Constant
                    && rhs is Float64Constant)
                {
                    Func<double, double, double?> impl;
                    if (doubleBinaryOps.TryGetValue(operatorName, out impl))
                    {
                        var maybeResult = impl(
                            ((Float64Constant)lhs).Value,
                            ((Float64Constant)rhs).Value);

                        if (maybeResult.HasValue)
                        {
                            result = new Float64Constant(maybeResult.Value);
                            return true;
                        }
                    }
                }
            }
            else if (arguments.Count == 1)
            {
                var operand = arguments[0];
                if (operatorName == Operators.Convert
                    && operand is IntegerConstant
                    && prototype.ResultType.IsIntegerType())
                {
                    result = ((IntegerConstant)operand).Cast(
                        prototype.ResultType.GetIntegerSpecOrNull());
                    return true;
                }
            }
            result = null;
            return false;
        }

        private static Dictionary<string, Func<double, double, double?>> doubleBinaryOps =
            new Dictionary<string, Func<double, double, double?>>()
        {
            { Operators.Add, (x, y) => x + y },
            { Operators.Subtract, (x, y) => x - y },
            { Operators.Multiply, (x, y) => x * y },
            { Operators.Divide, (x, y) => y == 0.0 ? (double?)null : x / y }
        };

        private static Dictionary<string, Func<IntegerConstant, IntegerConstant, IntegerConstant>> intBinaryOps =
            new Dictionary<string, Func<IntegerConstant, IntegerConstant, IntegerConstant>>()
        {
            { Operators.Add, (x, y) => x.Add(y) },
            { Operators.Subtract, (x, y) => x.Subtract(y) },
            { Operators.Multiply, (x, y) => x.Multiply(y) },
            { Operators.Divide, (x, y) => x.Divide(y) },
            { Operators.Remainder, (x, y) => x.Remainder(y) },
            { Operators.And, (x, y) => x.BitwiseAnd(y) },
            { Operators.Or, (x, y) => x.BitwiseOr(y) },
            { Operators.Xor, (x, y) => x.BitwiseXor(y) },
            { Operators.LeftShift, (x, y) => x.ShiftLeft(y) },
            { Operators.RightShift, (x, y) => x.ShiftRight(y) },
            { Operators.IsEqualTo, (x, y) => BooleanConstant.Create(x.Equals(y)) },
            { Operators.IsNotEqualTo, (x, y) => BooleanConstant.Create(!x.Equals(y)) },
            { Operators.IsLessThan, (x, y) => BooleanConstant.Create(x.CompareTo(y) < 0) },
            { Operators.IsGreaterThan, (x, y) => BooleanConstant.Create(x.CompareTo(y) > 0) },
            { Operators.IsLessThanOrEqualTo, (x, y) => BooleanConstant.Create(x.CompareTo(y) <= 0) },
            { Operators.IsGreaterThanOrEqualTo, (x, y) => BooleanConstant.Create(x.CompareTo(y) >= 0) },
        };

        private static IntegerSpec UnionIntSpecs(
            IntegerSpec firstSpec,
            params IntegerSpec[] otherSpecs)
        {
            var spec = firstSpec;
            foreach (var other in otherSpecs)
            {
                if (other.IsSigned != spec.IsSigned)
                {
                    IntegerSpec signed;
                    IntegerSpec unsigned;
                    if (other.IsSigned)
                    {
                        signed = other;
                        unsigned = spec;
                    }
                    else
                    {
                        signed = spec;
                        unsigned = other;
                    }

                    if (signed.Size <= unsigned.Size)
                    {
                        spec = new IntegerSpec(unsigned.Size + 1, true);
                    }
                    else
                    {
                        spec = signed;
                    }
                }
                else if (other.Size > spec.Size)
                {
                    spec = other;
                }
            }
            return spec;
        }

        private static bool AreCompatibleIntegers(
            IReadOnlyList<Constant> arguments,
            IntegerSpec initialSpec = null)
        {
            var spec = initialSpec;
            foreach (var arg in arguments)
            {
                if (arg is IntegerConstant)
                {
                    var constant = (IntegerConstant)arg;
                    if (spec == null)
                    {
                        spec = constant.Spec;
                    }
                    else if (!spec.Equals(constant.Spec))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsClosedIntOpApplication(
            IType resultType,
            IReadOnlyList<Constant> arguments)
        {
            var spec = resultType.GetIntegerSpecOrNull();
            if (spec == null)
            {
                return false;
            }
            else
            {
                return AreCompatibleIntegers(arguments, spec);
            }
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
            /// The left shift operator.
            /// </summary>
            public const string LeftShift = "shl";

            /// <summary>
            /// The right shift operator.
            /// </summary>
            public const string RightShift = "shr";

            /// <summary>
            /// The unary conversion operator.
            /// </summary>
            public const string Convert = "convert";

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
                        Xor,
                        LeftShift,
                        RightShift,

                        Convert
                    });

            /// <summary>
            /// Tells if a particular operator name refers to a standard
            /// relational operator.
            /// </summary>
            /// <param name="operatorName">The operator name to examine.</param>
            /// <returns>
            /// <c>true</c> if <paramref name="operatorName"/>
            /// is a standard relational operator;
            /// otherwise, <c>false</c>.
            /// </returns>
            public static bool IsRelationalOperator(string operatorName)
            {
                switch (operatorName)
                {
                    case IsGreaterThan:
                    case IsGreaterThanOrEqualTo:
                    case IsEqualTo:
                    case IsNotEqualTo:
                    case IsLessThan:
                    case IsLessThanOrEqualTo:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
