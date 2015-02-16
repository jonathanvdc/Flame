using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public interface IPartialBlock : IPythonBlock
    {
        IPythonBlock Complete(IPythonBlock[] Arguments);
    }

    public class PartialInvocationBlock : IPartialBlock
    {
        public PartialInvocationBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IType Type, params IPythonBlock[] PartialArguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.PartialArguments = PartialArguments;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IPythonBlock Target { get; private set; }
        public IPythonBlock[] PartialArguments { get; private set; }
        public IType Type { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Target.GetCode());
            cb.Append('(');
            for (int i = 0; i < PartialArguments.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                cb.Append(PartialArguments[i].GetCode());
            }
            return cb;
        }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            return new InvocationBlock(CodeGenerator, Target, PartialArguments.Concat(Arguments).ToArray(), Type);
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies().Union(PartialArguments.GetDependencies());
        }
    }
    public class PartialIndexedBlock : IPartialBlock
    {
        public PartialIndexedBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, AccessorType Operation, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Operation = Operation;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IPythonBlock Target { get; private set; }
        public AccessorType Operation { get; private set; }
        public IType Type { get; private set; }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            if (Operation.Equals(AccessorType.GetAccessor))
            {
                return new PythonIndexedBlock(CodeGenerator, Target, Arguments);
            }
            else if (Operation.Equals(AccessorType.SetAccessor))
            {
                return new AssignmentBlock(CodeGenerator, new PythonIndexedBlock(CodeGenerator, Target, Arguments.Take(Arguments.Length - 1).ToArray()), Arguments[Arguments.Length - 1]);
            }
            else
            {
                throw new NotSupportedException("Accessor types other than get or set are not supported for default indexing syntax.");
            }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Target.GetCode());
            cb.Append('[');
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies();
        }
    }
}
