using Flame.Compiler;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class InvariantImplementationMethod : IMethod, ICppMember, IEquatable<IMethod>
    {
        public InvariantImplementationMethod(TypeInvariants Invariants)
        {
            this.Invariants = Invariants;
        }

        public TypeInvariants Invariants { get; private set; }
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
            return new DescriptionAttribute("summary", "Checks if this type's invariants are being respected. A boolean value is returned that indicates whether this is indeed the case.");
        }

        private DescriptionAttribute CreateRemarks()
        {
            return new DescriptionAttribute("remarks", "This method should not be called directly. It should only be called from '" + Invariants.CheckInvariantsMethod.Name + "'.");
        }

        public IEnumerable<IAttribute> Attributes
        {
            get
            {
                var baseAttrs = new IAttribute[] 
                {
                    PrimitiveAttributes.Instance.ConstantAttribute,
                    CreateSummary(),
                    CreateRemarks()
                };
                if (DeclaringType.get_IsVirtual() || DeclaringType.get_IsInterface())
                {
                    return baseAttrs.With(new AccessAttribute(AccessModifier.Protected)).With(PrimitiveAttributes.Instance.VirtualAttribute);
                }
                else
                {
                    return baseAttrs.With(new AccessAttribute(AccessModifier.Private));
                }
            }
        }

        public string Name
        {
            get { return "CheckInvariantsCore"; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        #region ICppMember Implementation

        public bool InlineTestBlock
        {
            get
            {
                return !this.get_IsVirtual() && !BaseMethods.Any();
            }
        }

        public ICppBlock CreateTestBlock(ICodeGenerator CodeGenerator)
        {
            var allInvariants = Invariants.GetAllInvariants();
            var test = allInvariants.First();
            foreach (var item in allInvariants.Skip(1))
            {
                test = (ICppBlock)CodeGenerator.EmitLogicalAnd(test, item);
            }
            return test;
        }

        private CppMethod cppMethod;
        private int invariantCount;
        public CppMethod ToCppMethod()
        {
            if (cppMethod == null || invariantCount != Invariants.InvariantCount)
            {
                var method = new CppMethod(Invariants.DeclaringType, this, Environment);
                if (Invariants.HasInvariants)
                {
                    var codeGen = method.GetBodyGenerator();
                    
                    var allInvariants = Invariants.GetAllInvariants();
                    var test = allInvariants.First();
                    foreach (var item in allInvariants.Skip(1))
                    {
                        test = (ICppBlock)codeGen.EmitLogicalAnd(test, item);
                    }

                    var body = codeGen.EmitReturn(test);
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
