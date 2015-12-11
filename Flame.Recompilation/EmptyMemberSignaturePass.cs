using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    /// <summary>
    /// Defines a member signature pass that does nothing.
    /// </summary>
    public sealed class EmptyMemberSignaturePass<T> : IPass<MemberSignaturePassArgument<T>, MemberSignaturePassResult>
        where T : IMember
    {
        private EmptyMemberSignaturePass() { }

        public static readonly EmptyMemberSignaturePass<T> Instance = new EmptyMemberSignaturePass<T>();

        public MemberSignaturePassResult Apply(MemberSignaturePassArgument<T> Value)
        {
            return new MemberSignaturePassResult();
        }
    }
}
