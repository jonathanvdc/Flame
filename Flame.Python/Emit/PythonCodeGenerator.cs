using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonCodeGenerator : ICodeGenerator, IYieldCodeGenerator, IInitializingCodeGenerator, IForeachCodeGenerator
    {
        public PythonCodeGenerator(IMethod Method)
        {
            this.Method = Method;
            this.generatedNames = new List<string>();
        }

        public IMethod Method { get; private set; }

        #region Blocks

        public IBlockGenerator CreateBlock()
        {
            return new BlockGenerator(this);
        }

        public IBlockGenerator CreateDoWhileBlock(ICodeBlock Condition)
        {
            throw new NotImplementedException();
        }

        public IIfElseBlockGenerator CreateIfElseBlock(ICodeBlock Condition)
        {
            return new IfElseBlockGenerator(this, (IPythonBlock)Condition);
        }

        public IBlockGenerator CreateWhileBlock(ICodeBlock Condition)
        {
            return new WhileBlockGenerator(this, (IPythonBlock)Condition);
        }

        public ICollectionBlock CreateCollectionBlock(IVariableMember Member, ICodeBlock Collection)
        {
            var pyColl = (IPythonBlock)Collection;
            if (pyColl.Type.IsContainerType)
            {
                return new ListCollectionBlock(this, pyColl);
            }
            else
            {
                return new CollectionBlock(this, Member, pyColl);
            }
        }

        public IForeachBlockGenerator CreateForeachBlock(IEnumerable<ICollectionBlock> Collections)
        {
            return new ForeachBlockGenerator(this, Collections.Cast<IPythonCollectionBlock>());
        }

        #endregion

        #region Math

        public ICodeBlock EmitBinary(ICodeBlock A, ICodeBlock B, Operator Op)
        {
            return new BinaryOperation(this, (IPythonBlock)A, Op, (IPythonBlock)B);
        }

        public ICodeBlock EmitUnary(ICodeBlock Value, Operator Op)
        {
            return new UnaryOperation(this, (IPythonBlock)Value, Op);
        }

        #endregion

        #region Constants

        public ICodeBlock EmitBit16(ushort Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit32(uint Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit64(ulong Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBit8(byte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitBoolean(bool Value)
        {
            return new KeywordBlock(this, Value ? "True" : "False", PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitInt16(short Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt32(int Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt64(long Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitInt8(sbyte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt16(ushort Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt32(uint Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt64(ulong Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitUInt8(byte Value)
        {
            return new IntConstant(this, Value);
        }

        public ICodeBlock EmitNull()
        {
            return new KeywordBlock(this, "None", PrimitiveTypes.Null);
        }

        public ICodeBlock EmitChar(char Value)
        {
            return new StringConstant(this, Value.ToString());
        }

        public ICodeBlock EmitFloat32(float Value)
        {
            return new FloatConstant(this, Value);
        }

        public ICodeBlock EmitFloat64(double Value)
        {
            return new FloatConstant(this, Value);
        }

        public ICodeBlock EmitString(string Value)
        {
            return new StringConstant(this, Value);
        }

        #endregion

        #region Invocations

        public ICodeBlock EmitInvocation(ICodeBlock Method, IEnumerable<ICodeBlock> Arguments)
        {
            var target = (IPythonBlock)Method;
            if (target is PythonNonexistantBlock)
            {
                return target;
            }
            var pyArgs = Arguments.Cast<IPythonBlock>().ToArray();
            if (target is IPartialBlock)
            {
                return ((IPartialBlock)target).Complete(pyArgs);
            }
            else
            {
                var deleg = ((IMethod)target.Type);
                IType retType = deleg.IsConstructor ? deleg.DeclaringType : deleg.ReturnType;
                return new InvocationBlock(this, target, pyArgs, retType);
            }
        }

        public ICodeBlock EmitMethod(IMethod Method, ICodeBlock Caller)
        {
            if (Method.Equals(PythonObjectType.Instance.GetConstructor(new IType[0])))
            {
                return new PythonNonexistantBlock(this);
            }
            if (PythonPrimitiveMap.IsPrimitiveMethod(Method))
            {
                return PythonPrimitiveMap.CreatePrimitiveMethodAccess(this, Caller as IPythonBlock, Method);
            }
            if (Caller == null)
            {
                if (Method.IsConstructor)
                {
                    return new PythonIdentifierBlock(this, GetName(Method.DeclaringType), MethodType.Create(Method), ModuleDependency.FromType(Method.DeclaringType));
                }
                else
                {
                    return new PythonIdentifierBlock(this, GetName(Method), MethodType.Create(Method));
                }
            }
            else
            {
                if (Method is IAccessor)
                {
                    var acc = (IAccessor)Method;
                    if ((Method.DeclaringType.get_IsArray() || Method.DeclaringType.get_IsVector() || Method.DeclaringType.Equals(PrimitiveTypes.String)) && acc.DeclaringProperty.Name == "Length")
                    {
                        return new PartialInvocationBlock(this, new PythonIdentifierBlock(this, "len", PythonObjectType.Instance), Method.ReturnType, (IPythonBlock)Caller);
                    }
                    else if (acc.DeclaringProperty.get_IsIndexer() && acc.DeclaringProperty.Name == "this")
                    {
                        return new PartialIndexedBlock(this, (IPythonBlock)Caller, acc.AccessorType, Method.ReturnType);
                    }
                    else
                    {
                        return new PartialPropertyAccess(this, (IPythonBlock)Caller, acc);
                    }
                }
                else if (Method.IsConstructor)
                {
                    var ctorName = new MemberAccessBlock(this, new PythonIdentifierBlock(this, GetName(Method.DeclaringType), MethodType.Create(Method), ModuleDependency.FromType(Method.DeclaringType)), "__init__", PrimitiveTypes.Void);
                    return new PartialInvocationBlock(this, ctorName, PrimitiveTypes.Void, (IPythonBlock)Caller);
                }
                return new MemberAccessBlock(this, (IPythonBlock)Caller, GetName(Method), MethodType.Create(Method));
            }
        }

        #endregion

        #region GetName

        protected IMemberNamer GetMemberNamer()
        {
            return Method.DeclaringType.DeclaringNamespace.DeclaringAssembly.GetMemberNamer();
        }

        public string GetName(IMethod Method)
        {
            return GetMemberNamer().Name(Method);
        }

        public string GetName(IType Type)
        {
            return GetMemberNamer().Name(Type);
        }

        #endregion

        #region Conversions

        public static bool AreEquivalent(IType Source, IType Target)
        {
            if (Source.Equals(PrimitiveTypes.Char) && Target.Equals(PrimitiveTypes.String))
            {
                return true;
            }
            else if ((Source.get_IsBit() || Source.get_IsInteger()) && (Target.get_IsBit() || Target.get_IsInteger()))
            {
                return true;
            }
            else if (Source.get_IsFloatingPoint() && Target.get_IsFloatingPoint())
            {
                return true;
            }
            else if ((!Source.Equals(PrimitiveTypes.String) && Target.Equals(PrimitiveTypes.String)) || (Source.Equals(PrimitiveTypes.String) && !Target.Equals(PrimitiveTypes.String)))
            {
                return false;
            }
            else if (Source.get_IsReferenceType() && Target.get_IsReferenceType())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ICodeBlock EmitConversion(ICodeBlock Value, IType Type)
        {
            var pythonVal = (IPythonBlock)Value;
            var tVal = pythonVal.Type;
            string name;
            if (AreEquivalent(tVal, Type))
            {
                return new ImplicitlyConvertedBlock(this, pythonVal, Type); // No conversion necessary
            }
            else if ((tVal.get_IsInteger() || tVal.get_IsBit()) && Type.Equals(PrimitiveTypes.Char))
            {
                name = "chr";
            }
            else if ((Type.get_IsInteger() || Type.get_IsBit()) && tVal.Equals(PrimitiveTypes.Char))
            {
                name = "ord";
            }
            else if ((tVal.get_IsFloatingPoint() && Type.get_IsBit()) || (tVal.get_IsBit() && Type.get_IsFloatingPoint()))
            {
                return new FloatBitwiseConversionBlock(this, pythonVal, Type);
            }
            else
            {
                name = GetName(Type);
            }
            return new InvocationBlock(this, new PythonIdentifierBlock(this, name, PythonObjectType.Instance), new IPythonBlock[] { pythonVal }, Type);
        }

        #endregion

        #region Object Model

        public ICodeBlock EmitDefaultValue(IType Type)
        {
            if (Type.get_IsInteger() || Type.get_IsBit())
            {
                return EmitInt32(0);
            }
            else if (Type.get_IsFloatingPoint())
            {
                return EmitFloat64(0);
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return EmitBoolean(false);
            }
            else if (Type.get_IsValueType())
            {
                throw new NotImplementedException();
            }
            else
            {
                return EmitNull();
            }
        }

        public ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value)
        {
            return new InvocationBlock(this, new PythonIdentifierBlock(this, "isinstance", PythonObjectType.Instance), new IPythonBlock[] { (IPythonBlock)Value, new PythonIdentifierBlock(this, GetName(Type), PythonObjectType.Instance) }, PrimitiveTypes.Boolean);
        }

        public ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions)
        {
            var dimArr = Dimensions.Cast<IPythonBlock>().ToArray();
            var arrType = ElementType.MakeArrayType(dimArr.Length);
            if (dimArr.Length == 1)
            {
                return new BinaryOperation(this, new NewListBlock(this, arrType, (IPythonBlock)EmitDefaultValue(ElementType)), Operator.Multiply, dimArr[0]);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions)
        {
            return EmitNewArray(ElementType, Dimensions.Select((item) => EmitInt32(item)));
        }

        #endregion

        #region Local Name Generation

        private string GetSuggestedName(IType Type, params string[] NameSuggestions)
        {
            foreach (var item in NameSuggestions)
            {
                if (!LocalNameExists(item))
                {
                    return GetLocalName(item);
                }
            }
            return GetLocalName(Type);
        }

        private string GetIntegerName()
        {
            return GetSuggestedName(PrimitiveTypes.Int32, "i", "j", "k");
        }

        private string GetFloatName()
        {
            return GetSuggestedName(PrimitiveTypes.Float64, "x", "y", "z", "u", "v", "w");
        }

        public bool LocalNameExists(string Name)
        {
            return generatedNames.Contains(Name);
        }
        public string GetLocalName(string RequestedName)
        {
            return GetLocalName(RequestedName, false);
        }
        private string GetLocalName(string RequestedName, bool PreferNumbered)
        {
            int count = 0;
            string newName = PreferNumbered ? RequestedName + "_0" : RequestedName;
            while (LocalNameExists(newName))
            {
                newName = RequestedName + "_" + count;
                count++;
            }
            generatedNames.Add(newName);
            return newName;
        }
        public string GetLocalName(IType Type)
        {
            if (Type.get_IsInteger())
            {
                return GetIntegerName();
            }
            else if (Type.get_IsFloatingPoint())
            {
                return GetFloatName();
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return GetLocalName("flag");
            }
            return GetLocalName(PythonifyingMemberNamer.Pythonify(GetName(Type)));
        }
        public void ReleaseLocalName(string Name)
        {
            generatedNames.Remove(Name);
        }

        private List<string> generatedNames;

        #endregion

        #region Variables

        public IVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index)
        {
            return new PythonIndexedVariable(this, (IPythonBlock)Value, Index.Cast<IPythonBlock>().ToArray());
        }

        public IVariable GetField(IField Field, ICodeBlock Target)
        {
            return new PythonFieldVariable(this, (IPythonBlock)Target, Field);
        }

        public IVariable DeclareVariable(IVariableMember VariableMember)
        {
            return new PythonLocalVariable(this, VariableMember);
        }

        public IVariable GetArgument(int Index)
        {
            return new PythonArgumentVariable(this, Index);
        }

        public IVariable GetThis()
        {
            return new PythonThisVariable(this);
        }

        #endregion

        #region IYieldCodeGenerator Implementation

        public ICodeBlock EmitYieldBreak()
        {
            throw new NotImplementedException();
        }

        public ICodeBlock EmitYieldReturn(ICodeBlock Value)
        {
            return new YieldBlock(this, (IPythonBlock)Value);
        }

        #endregion

        #region IInitializingCodeGenerator Implementation

        public ICodeBlock EmitInitializedArray(IType ElementType, ICodeBlock[] Elements)
        {
            return new NewListBlock(this, ElementType.MakeArrayType(1), Elements.Cast<IPythonBlock>().ToArray());
        }

        public ICodeBlock EmitInitializedVector(IType ElementType, ICodeBlock[] Elements)
        {
            return new NewListBlock(this, ElementType.MakeVectorType(new int[] { Elements.Length }), Elements.Cast<IPythonBlock>().ToArray());
        }

        #endregion
    }
}
