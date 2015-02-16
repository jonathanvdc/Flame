using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public static class GenericConstraintExtensions
    {
        public static TypeConstraint Recompile(this TypeConstraint Constraint, AssemblyRecompiler Recompiler)
        {
            return new TypeConstraint(Recompiler.GetType(Constraint.Type));
        }

        public static AndConstraint Recompile(this AndConstraint Constraint, AssemblyRecompiler Recompiler)
        {
            return new AndConstraint(Constraint.Constraints.Select((item) => item.Recompile(Recompiler)).ToArray());
        }

        public static IGenericConstraint Recompile(this IGenericConstraint Constraint, AssemblyRecompiler Recompiler)
        {
            if (Constraint is TypeConstraint)
            {
                return Recompile((TypeConstraint)Constraint, Recompiler);
            }
            else if (Constraint is AndConstraint)
            {
                return Recompile((AndConstraint)Constraint, Recompiler);
            }
            else
            {
                return Constraint;
            }
        }
    }
}
