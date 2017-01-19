using System;
using System.Collections.Generic;

namespace Flame.Cecil
{
    /// <summary>
    /// Provides helper functionality that names operators
    /// and parses operator names.
    /// </summary>
    public static class CecilOperatorNames
    {
        /// <summary>
        /// The name assigned to an implicit user-defined conversion.
        /// </summary>
        public const string ImplicitConversionName = "op_Implicit";

        /// <summary>
        /// The name assigned to an explicit user-defined conversion.
        /// </summary>
        public const string ExplicitConversionName = "op_Explicit";

        /// <summary>
        /// The name assigned to the binary + operator.
        /// </summary>
        public const string AdditionOperatorName = "op_Addition";

        /// <summary>
        /// The name assigned to the binary &amp; operator.
        /// </summary>
        public const string BitwiseAndOperatorName = "op_BitwiseAnd";

        /// <summary>
        /// The name assigned to the unary ~ operator.
        /// </summary>
        public const string BitwiseComplementOperatorName = "op_OnesComplement";

        /// <summary>
        /// The name assigned to the binary | operator.
        /// </summary>
        public const string BitwiseOrOperatorName = "op_BitwiseOr";

        /// <summary>
        /// The name assigned to the binary concatentation operator.
        /// </summary>
        public const string ConcatenateOperatorName = "op_Concatenate";

        /// <summary>
        /// The name assigned to the unary -- operator.
        /// </summary>
        public const string DecrementOperatorName = "op_Decrement";

        /// <summary>
        /// The name assigned to the binary / operator.
        /// </summary>
        public const string DivisionOperatorName = "op_Division";

        /// <summary>
        /// The name assigned to the binary == operator.
        /// </summary>
        public const string EqualityOperatorName = "op_Equality";

        /// <summary>
        /// The name assigned to the binary ^ operator.
        /// </summary>
        public const string ExclusiveOrOperatorName = "op_ExclusiveOr";

        /// <summary>
        /// The name assigned to the binary &gt; operator.
        /// </summary>
        public const string GreaterThanOperatorName = "op_GreaterThan";

        /// <summary>
        /// The name assigned to the binary &gt;= operator.
        /// </summary>
        public const string GreaterThanOrEqualOperatorName = "op_GreaterThanOrEqual";

        /// <summary>
        /// The name assigned to the unary ++ operator.
        /// </summary>
        public const string IncrementOperatorName = "op_Increment";

        /// <summary>
        /// The name assigned to the binary != operator.
        /// </summary>
        public const string InequalityOperatorName = "op_Inequality";

        /// <summary>
        /// The name assigned to the signed binary &lt;&lt; operator.
        /// </summary>
        public const string LeftShiftOperatorName = "op_LeftShift";

        /// <summary>
        /// The name assigned to the binary &lt; operator.
        /// </summary>
        public const string LessThanOperatorName = "op_LessThan";

        /// <summary>
        /// The name assigned to the binary &lt;= operator.
        /// </summary>
        public const string LessThanOrEqualOperatorName = "op_LessThanOrEqual";

        /// <summary>
        /// The name assigned to the unary ! operator.
        /// </summary>
        public const string LogicalNotOperatorName = "op_LogicalNot";

        /// <summary>
        /// The name assigned to the binary &amp;&amp; operator.
        /// </summary>
        public const string LogicalAndOperatorName = "op_LogicalAnd";

        /// <summary>
        /// The name assigned to the binary || operator.
        /// </summary>
        public const string LogicalOrOperatorName = "op_LogicalOr";

        /// <summary>
        /// The name assigned to the binary % operator.
        /// </summary>
        public const string ModulusOperatorName = "op_Modulus";

        /// <summary>
        /// The name assigned to the binary * operator.
        /// </summary>
        public const string MultiplyOperatorName = "op_Multiply";

        /// <summary>
        /// The name assigned to the binary &gt;&gt; operator.
        /// </summary>
        public const string RightShiftOperatorName = "op_RightShift";

        /// <summary>
        /// The name assigned to the binary - operator.
        /// </summary>
        public const string SubtractionOperatorName = "op_Subtraction";

        /// <summary>
        /// The name assigned to the unary - operator.
        /// </summary>
        public const string UnaryNegationOperatorName = "op_UnaryNegation";

        /// <summary>
        /// The name assigned to the unary + operator.
        /// </summary>
        public const string UnaryPlusOperatorName = "op_UnaryPlus";

        /// <summary>
        /// Names the given binary operator. If the 
        /// operator could not be named, then null is
        /// returned.
        /// </summary>
        public static string NameBinaryOperator(Operator Op)
        {
            string result;
            if (binaryNameMap.TryGetValue(Op, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Names the given unary operator. If the 
        /// operator could not be named, then null is
        /// returned.
        /// </summary>
        public static string NameUnaryOperator(Operator Op)
        {
            string result;
            if (unaryNameMap.TryGetValue(Op, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Parses the given name as a binary operator name.
        /// If the name does not name a binary operator, then
        /// an undefined operator is returned. 
        /// </summary>
        public static Operator ParseBinaryOperatorName(string Name)
        {
            Operator result;
            if (binaryOpMap.TryGetValue(Name, out result))
                return result;
            else
                return Operator.Undefined;
        }

        /// <summary>
        /// Parses the given name as a unary operator name.
        /// If the name does not name a unary operator, then
        /// an undefined operator is returned. 
        /// </summary>
        public static Operator ParseUnaryOperatorName(string Name)
        {
            Operator result;
            if (unaryOpMap.TryGetValue(Name, out result))
                return result;
            else
                return Operator.Undefined;
        }

        /// <summary>
        /// Parses the given name as an operator name.
        /// If the name does not name a known operator, then
        /// an undefined operator is returned. 
        /// </summary>
        public static Operator ParseOperatorName(string Name)
        {
            Operator result;
            if (opMap.TryGetValue(Name, out result))
                return result;
            else
                return Operator.Undefined;
        }

        static CecilOperatorNames()
        {
            binaryNameMap = new Dictionary<Operator, string>()
            {
                { Operator.Add, AdditionOperatorName },
                { Operator.And, BitwiseAndOperatorName },
                { Operator.Concat, ConcatenateOperatorName },
                { Operator.Or, BitwiseOrOperatorName },
                { Operator.Divide, DivisionOperatorName },
                { Operator.CheckEquality, EqualityOperatorName },
                { Operator.Xor, ExclusiveOrOperatorName },
                { Operator.CheckGreaterThan, GreaterThanOperatorName },
                { Operator.CheckGreaterThanOrEqual, GreaterThanOrEqualOperatorName },
                { Operator.CheckInequality, InequalityOperatorName },
                { Operator.LeftShift, LeftShiftOperatorName },
                { Operator.CheckLessThan, LessThanOperatorName },
                { Operator.CheckLessThanOrEqual, LessThanOrEqualOperatorName },
                { Operator.LogicalAnd, LogicalAndOperatorName },
                { Operator.LogicalOr, LogicalOrOperatorName },
                { Operator.Remainder, ModulusOperatorName },
                { Operator.Multiply, MultiplyOperatorName },
                { Operator.RightShift, RightShiftOperatorName },
                { Operator.Subtract, SubtractionOperatorName }
            };
            unaryNameMap = new Dictionary<Operator, string>()
            {
                { Operator.Add, UnaryPlusOperatorName },
                { Operator.BitwiseComplement, BitwiseComplementOperatorName },
                { Operator.Decrement, DecrementOperatorName },
                { Operator.Increment, IncrementOperatorName },
                { Operator.Not, LogicalNotOperatorName },
                { Operator.Subtract, UnaryNegationOperatorName },
                { Operator.ConvertImplicit, ImplicitConversionName },
                { Operator.ConvertExplicit, ExplicitConversionName }
            };

            opMap = new Dictionary<string, Operator>();
            binaryOpMap = new Dictionary<string, Operator>();
            foreach (var item in binaryNameMap)
            {
                binaryOpMap[item.Value] = item.Key;
                opMap[item.Value] = item.Key;
            }

            unaryOpMap = new Dictionary<string, Operator>();
            foreach (var item in unaryNameMap)
            {
                unaryOpMap[item.Value] = item.Key;
                opMap[item.Value] = item.Key;
            }
        }
        
        private static readonly Dictionary<Operator, string> binaryNameMap;
        private static readonly Dictionary<Operator, string> unaryNameMap;
        private static readonly Dictionary<string, Operator> binaryOpMap;
        private static readonly Dictionary<string, Operator> unaryOpMap;
        private static readonly Dictionary<string, Operator> opMap;
    }
}

