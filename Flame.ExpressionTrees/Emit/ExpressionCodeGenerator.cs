using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class ExpressionCodeGenerator : ICodeGenerator, IInitializingCodeGenerator, IWhileCodeGenerator, IDoWhileCodeGenerator
    {
        public ExpressionCodeGenerator(ExpressionMethod Method)
        {
            this.Method = Method;
            this.retLabel = Expression.Label(Method.ExpressionReturnType);
            this.localVariables = new List<ParameterExpression>();
        }

        public ExpressionMethod Method { get; private set; }

        private LabelTarget retLabel;
        private List<ParameterExpression> localVariables;

        IMethod IMethodStructureGenerator.Method
        {
            get { return Method; }
        }

        #region Flow

        public ICodeBlock EmitTagged(BlockTag Tag, ICodeBlock Contents)
        {
            return new TaggedBlock(this, Tag, (IExpressionBlock)Contents);
        }

        public ICodeBlock EmitBreak(BlockTag Target)
        {
            return new BreakBlock(this, Target);
        }

        public ICodeBlock EmitContinue(BlockTag Target)
        {
            return new ContinueBlock(this, Target);
        }

        public ICodeBlock EmitReturn(ICodeBlock Value)
        {
            return new ReturnBlock(this, (IExpressionBlock)Value, retLabel);
        }

        public ICodeBlock EmitPop(ICodeBlock Value)
        {
            return EmitSequence(Value, EmitVoid());
        }

        public ICodeBlock EmitSequence(ICodeBlock First, ICodeBlock Second)
        {
            return new SequenceBlock(this, (IExpressionBlock)First, (IExpressionBlock)Second);
        }

        public ICodeBlock EmitIfElse(ICodeBlock Condition, ICodeBlock IfBody, ICodeBlock ElseBody)
        {
            var ifExpr = (IExpressionBlock)IfBody;

            return new ParentBlock(this, new IExpressionBlock[] { (IExpressionBlock)Condition, ifExpr, (IExpressionBlock)ElseBody }, ifExpr.Type, (exprs, flow) => Expression.Condition(exprs[0], exprs[1], exprs[2]));
        }

        public ICodeBlock EmitDoWhile(BlockTag Tag, ICodeBlock Body, ICodeBlock Condition)
        {
            return new DoWhileBlock(this, Tag, (IExpressionBlock)Condition, (IExpressionBlock)Body);
        }

        public ICodeBlock EmitWhile(BlockTag Tag, ICodeBlock Condition, ICodeBlock Body)
        {
            return new WhileBlock(this, Tag, (IExpressionBlock)Condition, (IExpressionBlock)Body);
        }

        #endregion

        #region Constants

        public ICodeBlock EmitVoid()
        {
            return new ExpressionBlock(this, Expression.Empty(), PrimitiveTypes.Void);
        }

        public ExpressionBlock EmitConstant(object Value, IType Type)
        {
            return new ExpressionBlock(this, Expression.Constant(Value), Type);
        }
        public ExpressionBlock EmitConstant(IBoundObject Value)
        {
            return EmitConstant(Value, Value.Type);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Bit64);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Bit8);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Float32);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Float64);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Char);
        }

        public ICodeBlock EmitString(string Value)
        {
            return EmitConstant(Value, PrimitiveTypes.String);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Int16);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Int32);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Int64);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return EmitConstant(Value, PrimitiveTypes.Int64);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return EmitConstant(Value, PrimitiveTypes.UInt16);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return EmitConstant(Value, PrimitiveTypes.UInt32);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return EmitConstant(Value, PrimitiveTypes.UInt64);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return EmitConstant(Value, PrimitiveTypes.UInt8);
        }

        public ICodeBlock EmitNull()
        {
            return EmitConstant(new NullExpression());
        }

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.get_IsReferenceType())
            {
                return EmitConversion(EmitNull(), Type);
            }

            var exprType = ExpressionTypeConverter.Instance.Convert(Type);

            if (exprType != typeof(IBoundObject))
            {
                return new ExpressionBlock(this, Expression.Default(exprType), Type);
            }

            throw new NotImplementedException();
        }

        #endregion

        #region Conversion

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            var val = (IExpressionBlock)Value;

            if (val.Type.Equals(PrimitiveTypes.Null))
            {
                return new ParentBlock(this, new[] { val }, Type, (exprs, flow) => exprs[0]);
            }
            else
            {
                var srcType = ExpressionTypeConverter.Instance.Convert(val.Type);
                var targetType = ExpressionTypeConverter.Instance.Convert(Type);
                if (srcType == typeof(IBoundObject) && targetType == typeof(IBoundObject)) // Reference conversion
                {
                    return new ReferenceConversionBlock(this, val, Type);
                }
                else if (srcType == typeof(IBoundObject)) // Unboxing conversion
                {
                    Expression<Func<IBoundObject, object>> quote = arg => arg.GetObjectValue();

                    return new ParentBlock(this, new IExpressionBlock[] { val }, Type, (exprs, flow) => Expression.Convert(Expression.Invoke(quote, exprs), targetType));
                }
                else if (targetType == typeof(IBoundObject))  // Boxing conversion
                {
                    Expression<Func<object, IBoundObject>> quote = arg => new ExpressionObject(arg, Type);

                    return new ParentBlock(this, new IExpressionBlock[] { val }, Type, (exprs, flow) => Expression.Invoke(quote, Expression.Convert(exprs[0], typeof(object))));
                }
                else // Value conversion
                {
                    return new ParentBlock(this, new IExpressionBlock[] { val }, Type, (exprs, flow) => Expression.Convert(exprs[0], targetType));
                }
            }
        }

        #endregion

        #region Boxing/Unboxing

        public static Expression Box(Expression Value, Type ExpressionType, IType Type)
        {
            Expression<Func<object, IBoundObject>> quote = arg => new ExpressionObject(Value, Type);

            return Expression.Invoke(quote, Expression.Convert(Value, ExpressionType));
        }

        public static Expression AutoBox(Expression Value, IType Type)
        {
            var type = ExpressionTypeConverter.Instance.Convert(Type);

            if (type == typeof(IBoundObject))
            {
                return Value;
            }
            else
            {
                return Box(Value, type, Type);
            }
        }

        public static Expression Unbox(Expression Value, Type ExpressionType, IType Type)
        {
            Expression<Func<IBoundObject, object>> quote = arg => arg.GetObjectValue();

            return Expression.Unbox(Expression.Invoke(quote, Value), ExpressionType);
        }

        public static Expression AutoUnbox(Expression Value, IType Type)
        {
            var exprType = ExpressionTypeConverter.Instance.Convert(Type);

            if (exprType == typeof(IBoundObject))
            {
                return Value; // Keep the boxed representation
            }
            else
            {
                return Unbox(Value, exprType, Type);
            }
        }

        public IExpressionBlock EmitAutoBox(IExpressionBlock Block, IType Type)
        {
            return new ParentBlock(this, new IExpressionBlock[] { Block }, Type, (exprs, flow) => AutoBox(exprs[0], Type));
        }

        #endregion

        #region IsIntrinsicType

        public static bool IsIntrinsicType(IType Type)
        {
            return ExpressionTypeConverter.Instance.Convert(Type) != typeof(IBoundObject);
        }

        #endregion

        #region Operators

        private static readonly Dictionary<Operator, ExpressionType> unaryExpressions = new Dictionary<Operator, ExpressionType>()
        {
            { Operator.Not, ExpressionType.Not },
            { Operator.Subtract, ExpressionType.Negate }
        };


        private static readonly Dictionary<Operator, ExpressionType> binaryExpressions = new Dictionary<Operator, ExpressionType>()
        {
            { Operator.Add, ExpressionType.Add },
            { Operator.And, ExpressionType.And },
            { Operator.Concat, ExpressionType.Add },            
            { Operator.CheckEquality, ExpressionType.Equal },
            { Operator.CheckInequality, ExpressionType.NotEqual },
            { Operator.CheckGreaterThan, ExpressionType.GreaterThan },
            { Operator.CheckGreaterThanOrEqual, ExpressionType.GreaterThanOrEqual },
            { Operator.CheckLessThan, ExpressionType.LessThan },
            { Operator.CheckLessThanOrEqual, ExpressionType.LessThanOrEqual },
            { Operator.Divide, ExpressionType.Divide },
            { Operator.LogicalAnd, ExpressionType.AndAlso },
            { Operator.LogicalOr, ExpressionType.OrElse },
            { Operator.LeftShift, ExpressionType.LeftShift },
            { Operator.Or, ExpressionType.Or },
            { Operator.Multiply, ExpressionType.Multiply },           
            { Operator.Subtract, ExpressionType.Subtract },
            { Operator.RightShift, ExpressionType.RightShift },
            { Operator.Remainder, ExpressionType.Modulo },
            { Operator.Xor, ExpressionType.ExclusiveOr }
        };

        private static readonly HashSet<ExpressionType> booleanBinaryExprs = new HashSet<ExpressionType>()
        {
            ExpressionType.AndAlso, 
            ExpressionType.OrElse,
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual
        };

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            var val = (IExpressionBlock)Value;

            if (!IsIntrinsicType(val.Type) || !unaryExpressions.ContainsKey(Op))
            {
                return null;
            }

            return new ParentBlock(this,
                new[] { val },
                val.Type,
                (exprs, flow) => Expression.MakeUnary(unaryExpressions[Op], exprs[0], exprs[0].Type));
        }

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var left = (IExpressionBlock)A;
            var right = (IExpressionBlock)B;

            if (!IsIntrinsicType(left.Type) || !IsIntrinsicType(right.Type) || !binaryExpressions.ContainsKey(Op))
            {
                return null;
            }

            var exprType = binaryExpressions[Op];

            return new ParentBlock(this,
                new[] { left, right },
                booleanBinaryExprs.Contains(exprType) ? PrimitiveTypes.Boolean : left.Type,
                (exprs, flow) => Expression.MakeBinary(exprType, exprs[0], exprs[1]));

        }

        #endregion

        #region Object Model

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            var val = (IExpressionBlock)Value;

            if (ExpressionTypeConverter.Instance.Convert(Type) != typeof(IBoundObject))
            {
                return EmitBoolean(val.Type.Is(Type));
            }

            if (val.Type.Is(Type))
            {
                return EmitBinary(val, EmitNull(), Operator.CheckInequality); // Emit a null check.
            }
            else
            {
                Expression<Func<IBoundObject, bool>> quote = arg => arg.Type == PrimitiveTypes.Null || arg.Type.Is(Type);

                return new ParentBlock(this,
                    new IExpressionBlock[] { val },
                    PrimitiveTypes.Boolean,
                    (exprs, flow) => Expression.Invoke(quote, AutoBox(exprs[0], Type)));
            }
        }

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            return new InvokeBlock(this, (IExpressionBlock)Method, Arguments.Cast<IExpressionBlock>());
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            return new MethodBlock(this, (IExpressionBlock)Caller, Method);
        }

        #endregion

        #region Arrays and Vectors

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            return new ParentBlock(this, Dimensions.Cast<IExpressionBlock>(), ElementType.MakeArrayType(Dimensions.Count()),
                (exprs, flow) => Expression.NewArrayBounds(ExpressionTypeConverter.Instance.Convert(ElementType), exprs.ToArray()));
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            return new ExpressionBlock(this,
                Expression.NewArrayBounds(ExpressionTypeConverter.Instance.Convert(ElementType), Dimensions.Select(item => Expression.Constant(item)).ToArray()),
                ElementType.MakeVectorType(Dimensions));
        }

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Items)
        {
            return new ParentBlock(this,
                Items.Cast<IExpressionBlock>(),
                ElementType.MakeArrayType(1),
                (exprs, flow) => Expression.NewArrayInit(ExpressionTypeConverter.Instance.Convert(ElementType), exprs));
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Items)
        {
            return new ParentBlock(this,
                Items.Cast<IExpressionBlock>(),
                ElementType.MakeVectorType(new int[] { Items.Length }),
                (exprs, flow) => Expression.NewArrayInit(ExpressionTypeConverter.Instance.Convert(ElementType), exprs));
        }

        #endregion

        #region Variables

        public IEmitVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            var val = (IExpressionBlock)Value;
            var exprVals = new IExpressionBlock[] { val }.Concat(Index.Cast<IExpressionBlock>());

            return new ExpressionVariable(this,
                new ParentBlock(this,
                    exprVals,
                    val.Type.GetEnumerableElementType(),
                    (exprs, flow) => Expression.ArrayAccess(exprs[0], exprs.Skip(1).ToArray())));
        }

        public IEmitVariable GetField(IField Field, ICodeBlock Target)
        {
            return new ExpressionFieldVariable(this, (IExpressionBlock)Target, Field);
        }

        public IEmitVariable DeclareVariable(IVariableMember VariableMember)
        {
            var localVar = Expression.Variable(ExpressionTypeConverter.Instance.Convert(VariableMember.VariableType), VariableMember.Name);

            localVariables.Add(localVar);

            return new ExpressionVariable(this,
                localVar,
                VariableMember.VariableType);
        }

        public IEmitVariable GetArgument(int Index)
        {
            return new ExpressionVariable(this,
                Method.ExpressionParameters[Index + (Method.IsStatic ? 0 : 1)],
                Method.GetParameters()[Index].ParameterType);
        }

        public IEmitVariable GetThis()
        {
            if (Method.IsStatic)
            {
                throw new InvalidOperationException("Cannot get a static method's 'this' parameter.");
            }
            return new ExpressionVariable(this, Method.ExpressionParameters[0], Method.DeclaringType);
        }

        #endregion

        #region EmitLambda

        public LambdaExpression EmitLambda(Expression Body)
        {
            return Expression.Lambda(
                        Expression.Block(
                            localVariables,
                            Body,
                            Expression.Label(retLabel, Expression.Default(retLabel.Type))),
                        Method.ExpressionParameters);
        }

        public LambdaExpression EmitLambda(IExpressionBlock Body)
        {
            return EmitLambda(Body.CreateExpression(FlowStructure.Root));
        }

        #endregion
    }
}
