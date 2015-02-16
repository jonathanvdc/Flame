using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class StackConstructorBlock : ICppBlock
    {
        public StackConstructorBlock(ICppBlock Constructor, params ICppBlock[] Arguments)
        {
            this.Constructor = Constructor;
            this.Arguments = Arguments;
        }
        public StackConstructorBlock(ICppBlock Constructor, IEnumerable<ICppBlock> Arguments)
        {
            this.Constructor = Constructor;
            this.Arguments = Arguments;
        }

        public ICppBlock Constructor { get; private set; }
        public IEnumerable<ICppBlock> Arguments { get; private set; }

        public IMethod Method
        {
            get
            {
                var type = Constructor.Type;
                return (IMethod)type;
            }
        }

        public IType Type
        {
            get
            {
                return Method.DeclaringType;
            }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Constructor.Dependencies.MergeDependencies(Arguments.SelectMany((item) => item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Constructor.LocalsUsed.Concat(Arguments.SelectMany((item) => item.LocalsUsed)).Distinct(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Constructor.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Constructor.GetCode();
            cb.Append('(');
            bool first = true;
            foreach (var item in Arguments)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cb.Append(", ");
                }
                cb.Append(item.GetCode());
            }
            cb.Append(')');
            return cb;
        }
    }
}
