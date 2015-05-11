using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class HashBlock : IInvocationBlock
    {
        public HashBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.UInt32; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies.MergeDependencies(new IHeaderDependency[] { StandardDependency.Functional }); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append("std::hash<");
            cb.Append(Value.Type.CreateBlock(CodeGenerator).GetCode());
            cb.Append(">()");
            var options = CodeGenerator.GetEnvironment().Log.Options;
            cb.AppendAligned(this.GetArgumentListCode(cb.LastCodeLine.Length, options));
            return cb;
        }

        public IEnumerable<ICppBlock> Arguments
        {
            get { return new ICppBlock[] { Value }; }
        }
    }
}
