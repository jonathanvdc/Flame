using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
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
            this.Attributes = new AttributeMap(new IAttribute[]
            {
                new AccessAttribute(AccessModifier.Public),
                PrimitiveAttributes.Instance.ConstantAttribute,
                CreateSummary()
            });
        }

        public TypeInvariants Invariants { get; private set; }
        public AttributeMap Attributes { get; private set; }
        public IType DeclaringType { get { return Invariants.DeclaringType; } }

        public IEnumerable<IMethod> BaseMethods
        {
            get
            {
                return DeclaringType.BaseTypes
                                    .Select(item => item.GetMethod(Name, IsStatic, ReturnType, Parameters.GetTypes().ToArray()))
                                    .Where(item => item != null);
            }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return new IParameter[0]; }
        }

        public bool IsConstructor
        {
            get { return false; }
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

        public string Name
        {
            get { return "CheckInvariants"; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        #region ICppMember Implementation

        private CppMethod cppMethod;
        private int invariantCount;
        public CppMethod ToCppMethod()
        {
            if (cppMethod == null || invariantCount != Invariants.InvariantCount)
            {
                var method = new CppMethod(Invariants.DeclaringType, new MethodPrototypeTemplate(this), Environment);
                if (Invariants.HasInvariants)
                {
                    var codeGen = method.GetBodyGenerator();

                    // if (!isCheckingInvariants)
                    // {
                    //     isCheckingInvariants = true;
                    //     bool result = <condition>;
                    //     isCheckingInvariants = false;
                    //     return result;
                    // }
                    // return true;

                    var fieldVar = codeGen.GetField(Invariants.IsCheckingInvariantsField, codeGen.GetThis().EmitGet());

                    var setBool = fieldVar.EmitSet(codeGen.EmitBoolean(true)); // isCheckingInvariants = true;
                    var resultVariable = codeGen.DeclareLocal(new UniqueTag(), new DescribedVariableMember("result", PrimitiveTypes.Boolean));
                    var checkImpl = Invariants.CheckInvariantsImplementationMethod;
                    var test = checkImpl.InlineTestBlock ? checkImpl.CreateTestBlock(codeGen) : codeGen.EmitInvocation(checkImpl, codeGen.GetThis().EmitGet(), Enumerable.Empty<ICodeBlock>());
                    var setResult = resultVariable.EmitSet(test); // bool result = <condition>;
                    var resetBool = fieldVar.EmitSet(codeGen.EmitBoolean(false)); // isCheckingInvariants = false;
                    var returnResult = codeGen.EmitReturn(resultVariable.EmitGet()); // return result;

                    var ifBody = codeGen.EmitSequence(setBool, codeGen.EmitSequence(setResult, codeGen.EmitSequence(resetBool, returnResult)));

                    var ifBlock = codeGen.EmitIfElse(codeGen.EmitNot(fieldVar.EmitGet()), ifBody, codeGen.EmitVoid());

                    var body = codeGen.EmitSequence(ifBlock, codeGen.EmitReturn(codeGen.EmitBoolean(true)));

                    method.SetMethodBody(body);
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
