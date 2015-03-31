using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppCodeGenerator : IUnmanagedCodeGenerator, IForeachCodeGenerator,
        IExceptionCodeGenerator, IForCodeGenerator, IInitializingCodeGenerator,
        IContractCodeGenerator
    {
        public CppCodeGenerator(IMethod Method, ICppEnvironment Environment)
        {
            this.Method = Method;
            this.Environment = Environment;
            this.LocalManager = new CppLocalManager(this);
            this.Contract = new MethodContract(this);
        }

        public MethodContract Contract { get; private set; }

        #region Properties

        public IMethod Method { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }

        #endregion

        #region Block Generators

        public IBlockGenerator CreateBlock()
        {
            return new CppBlockGenerator(this);
        }

        public IBlockGenerator CreateDoWhileBlock(ICodeBlock Condition)
        {
            return new DoWhileBlockGenerator(this, (ICppBlock)Condition);
        }

        public ICodeBlock CreateIfElseBlock(ICodeBlock Condition, ICodeBlock IfBlock, ICodeBlock ElseBlock)
        {
            var cppCond = (ICppBlock)Condition;
            var cppIf = (ICppBlock)IfBlock;
            var cppElse = (ICppBlock)ElseBlock;

            return new IfElseBlock(this, cppCond, cppIf, cppElse);
        }

        public IBlockGenerator CreateWhileBlock(ICodeBlock Condition)
        {
            return new WhileBlockGenerator(this, (ICppBlock)Condition);
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            var left = (ICppBlock)A;
            var right = (ICppBlock)B;
            if (left.IsZeroLiteral() && right.Type.get_IsPrimitive())
            {
                return new UnaryOperation(this, right, Operator.Subtract);
            }
            else if (BinaryOperation.IsSupported(Op, left.Type, right.Type))
            {
                return new BinaryOperation(this, left, Op, right);
            }
            else
            {
                return null; // Not supported
            }
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            return new UnaryOperation(this, (ICppBlock)Value, Op);
        }

        #endregion

        #region Literals

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new LiteralBlock(this, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new LiteralBlock(this, "nullptr", PrimitiveTypes.Null);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new CharLiteral(this, Value);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new StringLiteral(this, Value);
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new FloatLiteralBlock(this, Value);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new DoubleLiteralBlock(this, Value);
        }

        public ICodeBlock EmitBit16(ushort Value)
        {
            return EmitConversion(EmitUInt16(Value), PrimitiveTypes.Bit16);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return EmitConversion(EmitUInt32(Value), PrimitiveTypes.Bit32);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return EmitConversion(EmitUInt64(Value), PrimitiveTypes.Bit64);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return EmitConversion(EmitUInt8(Value), PrimitiveTypes.Bit8);
        }

        #endregion

        #region Object Model

        #region Conversions

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            var cppVal = (ICppBlock)Value;
            return new ConversionBlock(this, cppVal, Environment.TypeConverter.Convert(Type));
        }

        #endregion

        #region Default Values

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.get_IsPrimitive())
            {
                if (Type.get_IsInteger() || Type.get_IsBit())
                {
                    return new LiteralBlock(this, "0", Type);
                }
                else if (Type.get_IsFloatingPoint())
                {
                    return new LiteralBlock(this, "0.0", Type);
                }
                else if (Type.Equals(PrimitiveTypes.Char))
                {
                    return EmitChar(default(char));
                }
                else if (Type.Equals(PrimitiveTypes.Boolean))
                {
                    return EmitBoolean(false);
                }
            }
            if (Type.get_IsReferenceType() || Type.Equals(PrimitiveTypes.String))
            {
                return EmitNull();
            }
            else
            {
                return new RetypedBlock(new StackConstructorBlock(Type.CreateBlock(this), new ICppBlock[0]), Type);
            }
        }

        #endregion

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            var cppArgs = Arguments.Cast<ICppBlock>();
            if (Method is IPartialBlock)
            {
                return ((IPartialBlock)Method).Complete(new PartialArguments(cppArgs.ToArray()));
            }
            else
            {
                return new InvocationBlock((ICppBlock)Method, cppArgs);
            }
        }

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new IsInstanceBlock(this, (ICppBlock)Value, Environment.TypeConverter.Convert(Type));
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            if (Caller == null)
            {
                if (Method.IsConstructor)
                {
                    var resultType = Environment.TypeConverter.Convert(Method.DeclaringType);
                    if (!resultType.get_IsValueType())
                    {
                        return new PartialSharedPtrBlock(Method.CreateConstructorBlock(this));
                    }
                    else
                    {
                        return new PartialStackConstructorBlock(Method.CreateConstructorBlock(this));
                    }
                }
                else
                {
                    return Method.CreateBlock(this);
                }
            }
            else if (Method.IsConstructor && Method.GetParameters().Length == 0)
            {
                return new PartialEmptyBlock(this, MethodType.Create(Method)); // Do not emit calls to the parameterless base constructor
            }
            else
            {
                var cppCaller = (ICppBlock)Caller;
                if (Method is IAccessor)
                {
                    var accessor = (IAccessor)Method;
                    var declProp = accessor.DeclaringProperty;
                    if (cppCaller.Type.get_IsArray())
                    {
                        var arrSliceProp = cppCaller.Type.GetProperties().FirstOrDefault((item) => item.Name == declProp.Name && item.IsStatic == declProp.IsStatic);
                        if (arrSliceProp != null)
                        {
                            var arrSliceAccessor = arrSliceProp.GetAccessor(accessor.AccessorType);
                            if (arrSliceAccessor != null)
                            {
                                return new MemberAccessBlock(cppCaller, arrSliceAccessor, MethodType.Create(arrSliceAccessor));
                            }
                        }
                    }
                    else if (declProp.get_IsIndexer() && Method.DeclaringType.IsForeign() && declProp.GetIndexerParameters().Length == 1)
                    {
                        if (accessor.get_IsGetAccessor())
                        {
                            return new PartialElementBlock(cppCaller, Method.ReturnType);
                        }
                        else if (accessor.get_IsSetAccessor())
                        {
                            return new PartialSetElementBlock(cppCaller, declProp.PropertyType);
                        }
                    }
                }
                return new MemberAccessBlock(cppCaller, Method, MethodType.Create(Method));
            }
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            var ctor = Plugs.StdxArraySlice.Instance.MakeGenericType(new IType[] { ElementType }).GetConstructor(new IType[] { PrimitiveTypes.Int32 }, false);
            return EmitInvocation(EmitMethod(ctor, null), Dimensions);
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Variables

        #region Locals

        public CppLocalManager LocalManager { get; private set; }

        public IVariable DeclareVariable(IVariableMember VariableMember)
        {
            return DeclareUnmanagedVariable(VariableMember);
        }

        public IVariable DeclareNewVariable(IVariableMember VariableMember)
        {
            return DeclareNewUnmanagedVariable(VariableMember);
        }

        public IUnmanagedVariable DeclareUnmanagedVariable(IVariableMember VariableMember)
        {
            return LocalManager.Declare(this.ConvertVariableMember(VariableMember));
        }

        public IUnmanagedVariable DeclareNewUnmanagedVariable(IVariableMember VariableMember)
        {
            return LocalManager.DeclareNew(this.ConvertVariableMember(VariableMember));
        }

        #endregion

        public IVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return GetUnmanagedElement(Value, Index);
        }

        public IVariable GetField(IField Field, ICodeBlock Target)
        {
            return GetUnmanagedField(Field, Target);
        }

        public IVariable GetArgument(int Index)
        {
            return GetUnmanagedArgument(Index);
        }

        public IVariable GetThis()
        {
            return GetUnmanagedThis();
        }

        public IUnmanagedVariable GetUnmanagedElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new CppElement(this, (ICppBlock)Value, (ICppBlock)Index.Single());
        }

        public IUnmanagedVariable GetUnmanagedField(IField Field, ICodeBlock Target)
        {
            return new CppField(this, (ICppBlock)Target, Field);
        }

        public IUnmanagedVariable GetUnmanagedArgument(int Index)
        {
            return new CppArgument(this, Index);
        }

        public IUnmanagedVariable GetUnmanagedThis()
        {
            return new CppThis(this);
        }

        #endregion

        #region Unmanaged

        public ICodeBlock EmitDereferencePointer(ICodeBlock Pointer)
        {
            return new DereferenceBlock((ICppBlock)Pointer);
        }

        public ICodeBlock EmitSizeOf(IType Type)
        {
            return new InvocationBlock(new LiteralBlock(this, "sizeof", PrimitiveTypes.Void), new ICppBlock[] { Type.CreateBlock(this) });
        }

        public ICodeBlock EmitStoreAtAddress(ICodeBlock Pointer, ICodeBlock Value)
        {
            return new ExpressionStatementBlock(new VariableAssignmentBlock((ICppBlock)EmitDereferencePointer(Pointer), (ICppBlock)Value));
        }

        #endregion

        #region Foreach

        public ICollectionBlock CreateCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            return new CollectionBlock(this, Member, (ICppBlock)Collection);
        }

        public IForeachBlockGenerator CreateForeachBlock(IEnumerable<ICollectionBlock> Collections)
        {
            if (Collections.Count() == 1)
            {
                var singleCollection = Collections.Single() as CollectionBlock;
                if (singleCollection != null)
                {
                    return new ForeachBlockGenerator(this, singleCollection);
                }
            }
            return null;
        }

        #endregion

        #region IExceptionCodeGenerator

        public ITryBlockGenerator CreateTryBlock()
        {
            return new TryBlockGenerator(this);
        }

        public ICodeBlock EmitAssert(ICodeBlock Condition)
        {
            var cppExpr = (ICppBlock)EmitInvocation(EmitMethod(CppPrimitives.AssertMethod, null), new ICodeBlock[] { Condition });
            return new ImplicitDependencyBlock(new ExpressionStatementBlock(cppExpr), new IHeaderDependency[] { StandardDependency.CAssert });
        }

        public ICodeBlock EmitThrow(ICodeBlock Exception)
        {
            return new ThrowBlock(this, (ICppBlock)Exception);
        }

        #endregion

        #region IForCodeGenerator

        public IBlockGenerator CreateForBlock(ICodeBlock Initialization, ICodeBlock Condition, ICodeBlock Delta)
        {
            var cppInit = (ICppBlock)Initialization;
            var cppCond = (ICppBlock)Condition;
            var cppDelta = (ICppBlock)Delta;
            if (cppInit.IsSimple() && cppDelta.IsSimple())
            {
                return new ForBlockGenerator(this, cppInit, cppCond, cppDelta);
            }
            return null;
        }

        #endregion

        #region IInitializingCodeGenerator

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Items)
        {
            var initList = new InitializerListBlock(this, ElementType, Items.Cast<ICppBlock>());
            var ctor = Plugs.StdxArraySlice.Instance.MakeGenericType(new IType[] { ElementType }).GetConstructor(new IType[] { initList.Type }, false);
            return EmitInvocation(EmitMethod(ctor, null), new ICodeBlock[] { initList });
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Items)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IContractCodeGenerator

        private IVariable retVar;

        public IVariable ReturnVariable
        {
            get
            {
                if (retVar == null)
                {
                    retVar = LocalManager.DeclareNew(new DescribedVariableMember("result", Method.ReturnType));
                }
                return retVar;
            }
        }

        #endregion
    }
}
