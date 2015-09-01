using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class TypeInvariants
    {
        public TypeInvariants(IGenericResolverType DeclaringType, ICppEnvironment Environment)
        {
            this.DeclaringType = DeclaringType;
            this.Environment = Environment;
            this.CheckInvariantsImplementationMethod = new InvariantImplementationMethod(this);
            this.CodeGenerator = new CppCodeGenerator(CheckInvariantsImplementationMethod, Environment);
            this.invariants = new List<ICppBlock>();

            if (CheckInvariantsImplementationMethod.BaseMethods.Length == 0)
            {
                this.CheckInvariantsMethod = new InvariantMethod(this);
                var descField = new DescribedField("isCheckingInvariants", PrimitiveTypes.Boolean, false);
                descField.DeclaringType = DeclaringType;
                descField.AddAttribute(new AccessAttribute(AccessModifier.Private));
                this.IsCheckingInvariantsField = new CppField(DeclaringType, descField, Environment);
                this.IsCheckingInvariantsField.SetValue(new BooleanExpression(false));
                this.IsCheckingInvariantsField.IsMutable = true;
            }
            else
            {
                var parent = DeclaringType.GetParent();
                this.CheckInvariantsMethod = parent.GetInvariantsCheckMethod();
                this.IsCheckingInvariantsField = parent.GetIsCheckingInvariantsField();
            }
        }

        public IGenericResolverType DeclaringType { get; private set; }
        public CppCodeGenerator CodeGenerator { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        public InvariantMethod CheckInvariantsMethod { get; private set; }
        public InvariantImplementationMethod CheckInvariantsImplementationMethod { get; private set; }
        public CppField IsCheckingInvariantsField { get; private set; }

        private List<ICppBlock> invariants;

        public IEnumerable<ICppBlock> Invariants { get { return invariants; } }
        public bool HasInvariants { get { return InvariantCount > 0; } }
        public int InvariantCount { get { return invariants.Count; } }
        public bool HasAnyInvariants { get { return HasInvariants || InheritsInvariants; } }
        public bool InheritsInvariants { get { return CheckInvariantsImplementationMethod.BaseMethods.Any(); } }

        public IEnumerable<ICppBlock> GetInheritedInvariants()
        {
            List<ICodeBlock> results = new List<ICodeBlock>();
            var cg = CodeGenerator;
            foreach (var item in CheckInvariantsImplementationMethod.BaseMethods)
            {
                results.Add(cg.EmitInvocation(item, cg.GetThis().EmitGet(), Enumerable.Empty<ICodeBlock>()));
            }
            return results.Cast<ICppBlock>();
        }

        public IEnumerable<ICppBlock> GetAllInvariants()
        {
            return GetInheritedInvariants().Concat(Invariants);
        }

        public void AddInvariant(ICppBlock Condition)
        {
            invariants.Add(Condition);
        }
    }
}
