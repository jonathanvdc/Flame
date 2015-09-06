using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PythonIndexedBlock : IPythonBlock
    {
        public PythonIndexedBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IPythonBlock[] Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Arguments = Arguments;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IPythonBlock Target { get; private set; }
        public IPythonBlock[] Arguments { get; private set; }

        public IType Type
        {
            get
            {
                var targetType = Target.Type;
                if (targetType.Equals(PrimitiveTypes.String))
                {
                    return PrimitiveTypes.Char;
                }
                else if (targetType.get_IsContainerType())
                {
                    return targetType.AsContainerType().ElementType;
                }
                else
                {
                    return Target.Type.GetBestIndexer(false, Arguments.Select((item) => item.Type)).PropertyType;
                }
            }
        }

        public CodeBuilder GetCode()
        {
            var cb = Target.GetCode();
            cb.Append('[');
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                cb.Append(Arguments[i].GetCode());
            }
            cb.Append(']');
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies().Union(Arguments.GetDependencies());
        }
    }
}
