using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class SizeOfBlock : IInvocationBlock
    {
        public SizeOfBlock(ICodeGenerator CodeGenerator, ICppBlock SizeTypeBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.SizeTypeBlock = SizeTypeBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICppBlock SizeTypeBlock { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.UInt32; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return SizeTypeBlock.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Enumerable.Empty<CppLocal>(); }
        }        

        public IEnumerable<ICppBlock> Arguments
        {
            get { return new ICppBlock[] { SizeTypeBlock }; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("sizeof");
            var options = CodeGenerator.GetEnvironment().Log.Options;
            cb.AppendAligned(this.GetArgumentListCode(cb.LastCodeLine.Length, options));
            return cb;
        }
    }
}
