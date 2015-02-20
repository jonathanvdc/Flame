using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class InvocationBlock : IInvocationBlock
    {
        public InvocationBlock(ICppBlock Member, params ICppBlock[] Arguments)
            : this(Member, (IEnumerable<ICppBlock>)Arguments)
        { }
        public InvocationBlock(ICppBlock Member, IEnumerable<ICppBlock> Arguments)
        {
            this.Member = Member;
            this.Arguments = Arguments;
        }

        public ICppBlock Member { get; private set; }
        public IEnumerable<ICppBlock> Arguments { get; private set; }

        public IMethod Method
        {
            get
            {
                var type = Member.Type;
                return (IMethod)type;
            }
        }

        public IType Type
        {
            get 
            {
                return Method.ReturnType;
            }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Member.Dependencies.MergeDependencies(Arguments.SelectMany((item) => item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Member.LocalsUsed.Concat(Arguments.SelectMany((item) => item.LocalsUsed)).Distinct(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Member.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var cb = Member.GetCode();
            cb.Append(this.GetArgumentListCode());
            return cb;
        }
    }
}
