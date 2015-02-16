using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ElementBlock : ICppBlock
    {
        public ElementBlock(ICppBlock Target, ICppBlock Index, IType Type)
        {
            this.Target = Target;
            this.Index = Index;
            this.Type = Type;
        }

        public ICppBlock Target { get; private set; }
        public ICppBlock Index { get; private set; }
        public IType Type { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(Index.Dependencies).MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed.Concat(Index.LocalsUsed).Distinct(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Target.GetCode();
            cb.Append('[');
            cb.Append(Index.GetCode());
            cb.Append(']');
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
