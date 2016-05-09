using Flame.Compiler;
using Flame.Compiler.Build;
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
            this.attrMap = new Lazy<AttributeMap>(CreateAttributes);
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

        private AttributeMap CreateAttributes()
        {
            var results = new AttributeMapBuilder();
            results.Add(PrimitiveAttributes.Instance.ConstantAttribute);
            results.Add(CreateSummary());
            results.Add(CreateRemarks());
            if (DeclaringType.GetIsVirtual() || DeclaringType.GetIsInterface())
            {
                results.Add(new AccessAttribute(AccessModifier.Protected));
                results.Add(PrimitiveAttributes.Instance.VirtualAttribute);
            }
            else
            {
                results.Add(new AccessAttribute(AccessModifier.Private));
            }
            return new AttributeMap(results);
        }

        private Lazy<AttributeMap> attrMap;
        public AttributeMap Attributes
        {
            get
            {
                return attrMap.Value;
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
                return !this.GetIsVirtual() && !BaseMethods.Any();
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
                var method = new CppMethod(Invariants.DeclaringType, new MethodPrototypeTemplate(this), Environment);
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
