using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class InvariantMethod : IMethod, ICppMember, IEquatable<IMethod>
    {
        public InvariantMethod(TypeInvariants Invariants)
        {
            this.Invariants = Invariants;
        }

        public TypeInvariants Invariants { get; private set; }
        public IType DeclaringType { get { return Invariants.DeclaringType; } }

        public IMethod[] GetBaseMethods()
        {
            return DeclaringType.GetBaseTypes()
                .Select(item => item.GetMethod(Name, IsStatic, ReturnType, GetParameters().GetTypes()))
                .Where(item => item != null)
                .ToArray();
        }

        public IMethod GetGenericDeclaration()
        {
            return this;
        }

        public IParameter[] GetParameters()
        {
            return new IParameter[0];
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return this;
        }

        public IType ReturnType
        {
            get { return PrimitiveTypes.Boolean; }
        }

        public bool IsStatic
        {
            get { return false; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        private DescriptionAttribute CreateSummary()
        {
            return new DescriptionAttribute("summary", "Checks if this type's invariants are being respected. A boolean value is returned that indicates whether this is indeed the case. This method is publically visible, and can be used to verify an instance's state.");
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] 
            { 
                new AccessAttribute(AccessModifier.Public),
                PrimitiveAttributes.Instance.ConstantAttribute,
                CreateSummary()
            };
        }

        public string Name
        {
            get { return "CheckInvariants"; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return Enumerable.Empty<IType>();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Enumerable.Empty<IGenericParameter>();
        }

        #region ICppMember Implementation

        private CppMethod cppMethod;
        private int invariantCount;
        public CppMethod ToCppMethod()
        {
            if (cppMethod == null || invariantCount != Invariants.InvariantCount)
            {
                var method = new CppMethod(Invariants.DeclaringType, this, Environment);
                if (Invariants.HasInvariants)
                {
                    var bodyGen = method.GetBodyGenerator();
                    var codeGen = bodyGen.CodeGenerator;

                    // if (!isCheckingInvariants)
                    // {
                    //     isCheckingInvariants = true;
                    //     bool result = <condition>;
                    //     isCheckingInvariants = false;
                    //     return result;
                    // }
                    // return true;

                    var fieldVar = codeGen.GetField(Invariants.IsCheckingInvariantsField, codeGen.GetThis().CreateGetExpression().Emit(codeGen));

                    var ifBody = codeGen.CreateBlock();

                    fieldVar.CreateSetStatement(new BooleanExpression(true)).Emit(ifBody); // isCheckingInvariants = true;
                    var resultVariable = codeGen.DeclareVariable(new DescribedVariableMember("result", PrimitiveTypes.Boolean));
                    var checkImpl = Invariants.CheckInvariantsImplementationMethod;
                    var test = checkImpl.InlineTestBlock  ? checkImpl.CreateTestBlock(codeGen) : codeGen.EmitInvocation(checkImpl, codeGen.GetThis().CreateGetExpression().Emit(codeGen), Enumerable.Empty<ICodeBlock>());
                    resultVariable.CreateSetStatement(new CodeBlockExpression(test, PrimitiveTypes.Boolean)).Emit(ifBody); // bool result = <condition>;
                    fieldVar.CreateSetStatement(new BooleanExpression(false)).Emit(ifBody); // isCheckingInvariants = false;
                    ifBody.EmitReturn(resultVariable.CreateGetExpression().Emit(codeGen)); // return result;

                    var ifBlock = codeGen.CreateIfElseBlock(codeGen.EmitNot(fieldVar.CreateGetExpression().Emit(codeGen)), ifBody, codeGen.CreateBlock());

                    bodyGen.EmitBlock(ifBlock);
                    bodyGen.EmitReturn(codeGen.EmitBoolean(true));
                }
                invariantCount = Invariants.InvariantCount;
                cppMethod = method;
            }
            return cppMethod;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Invariants.Invariants.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.Union(b.Dependencies)); }
        }

        public ICppEnvironment Environment
        {
            get { return Invariants.Environment; }
        }

        public CodeBuilder GetHeaderCode()
        {
            return ToCppMethod().GetHeaderCode();
        }

        public bool HasSourceCode
        {
            get { return true; }
        }

        public CodeBuilder GetSourceCode()
        {
            return ToCppMethod().GetSourceCode();
        }

        #endregion

        #region Equality

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ DeclaringType.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is IMethod)
            {
                return this.Equals((IMethod)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(IMethod Other)
        {
            return CppMethod.Equals(this, Other);
        }

        #endregion
    }
}
