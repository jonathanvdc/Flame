using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// Represents a single member initialization in an initialization list.
    /// </summary>
    public class MemberInitializationBlock : INewObjectBlock
    {
        public MemberInitializationBlock(ICodeGenerator CodeGenerator, ICppBlock Target, params ICppBlock[] Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Arguments = Arguments;
        }
        public MemberInitializationBlock(ICodeGenerator CodeGenerator, ICppBlock Target, IEnumerable<ICppBlock> Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Arguments = Arguments;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICppBlock Target { get; private set; }
        public IEnumerable<ICppBlock> Arguments { get; private set; }

        public IType Type
        {
            get { return Target.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(Arguments.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed.Union(Arguments.GetUsedLocals()); }
        }

        public AllocationKind Kind
        {
            get { return AllocationKind.Stack; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Target.GetCode();
            var options = CodeGenerator.GetEnvironment().Log.Options;
            int offset = cb.LastCodeLine.Length;
            cb.AppendAligned(this.GetInitializationListCode(false, offset, options));
            return cb;
        }
    }
}
