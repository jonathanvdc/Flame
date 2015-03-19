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
            this.CheckInvariantsMethod = new InvariantMethod(this);
            this.CodeGenerator = new CppCodeGenerator(CheckInvariantsMethod, Environment);
            this.invariants = new List<ICppBlock>();

            var descField = new DescribedField("isCheckingInvariants", PrimitiveTypes.Boolean, false);
            descField.DeclaringType = DeclaringType;
            descField.AddAttribute(new AccessAttribute(AccessModifier.Private));
            this.IsCheckingInvariantsField = new CppField(DeclaringType, descField, Environment);
            this.IsCheckingInvariantsField.SetValue(new BooleanExpression(false));
            this.IsCheckingInvariantsField.IsMutable = true;
        }

        public IGenericResolverType DeclaringType { get; private set; }
        public CppCodeGenerator CodeGenerator { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        public InvariantMethod CheckInvariantsMethod { get; private set; }
        public CppField IsCheckingInvariantsField { get; private set; }

        private List<ICppBlock> invariants;

        public IEnumerable<ICppBlock> Invariants { get { return invariants; } }
        public bool HasInvariants { get { return InvariantCount > 0; } }
        public int InvariantCount { get { return invariants.Count; } }
        public bool HasAnyInvariants { get { return HasInvariants || CheckInvariantsMethod.GetBaseMethods().Any(); } }

        public IEnumerable<ICppBlock> GetInheritedInvariants()
        {
            List<ICodeBlock> results = new List<ICodeBlock>();
            var cg = CodeGenerator;
            foreach (var item in CheckInvariantsMethod.GetBaseMethods())
            {
                results.Add(cg.EmitInvocation(item, cg.GetThis().CreateGetExpression().Emit(cg), Enumerable.Empty<ICodeBlock>()));
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
