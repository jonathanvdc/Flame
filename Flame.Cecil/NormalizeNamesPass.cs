using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    /// <summary>
    /// A signature pass that normalizes member names 
    /// such that they fit the CLR naming scheme.
    /// </summary>
    public sealed class NormalizeNamesPass : IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>
    {
        private NormalizeNamesPass() { }

        /// <summary>
        /// Gets the one and only instance of this pass.
        /// </summary>
        public static readonly NormalizeNamesPass Instance = new NormalizeNamesPass();

        /// <summary>
        /// Stores the CLR name normalizing pass' name.
        /// </summary>
        public const string NormalizeNamesPassName = "normalize-names-clr";

        public MemberSignaturePassResult Apply(MemberSignaturePassArgument<IMember> Value)
        {
            var member = Value.Member;
            if (member is IProperty)
            {
                return new MemberSignaturePassResult(RenameProperty((IProperty)member));
            }
            else if (member is IAccessor)
            {
                return new MemberSignaturePassResult(RenameAccessor((IAccessor)member));
            }
            else if (member is IMethod)
            {
                return new MemberSignaturePassResult(RenameMethod((IMethod)member));
            }
            
            return new MemberSignaturePassResult();
        }

        private static string RenameMethod(IMethod Method)
        {
            if (!Method.IsConstructor)
            {
                return null;
            }
            else if (Method.IsStatic)
            {
                return ".cctor";
            }
            else
            {
                return ".ctor";
            }
        }

        private static string RenameProperty(IProperty Property)
        {
            if (Property.GetIsIndexer())
            {
                return "Item";
            }
            else
            {
                return null;
            }
        }

        private static string RenameAccessor(IAccessor Accessor)
        {
            return Accessor.AccessorType.Name.ToLower() + "_" + (RenameProperty(Accessor.DeclaringProperty) ?? Accessor.Name);
        }
    }
}
