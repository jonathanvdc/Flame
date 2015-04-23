using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class SequenceBlock : IPythonBlock
    {
        public SequenceBlock(IPythonBlock First, IPythonBlock Second)
        {
            this.First = First;
            this.Second = Second;
        }

        public IPythonBlock First { get; private set; }
        public IPythonBlock Second { get; private set; }

        public IType Type
        {
            get 
            {
                var type2 = Second.Type;
                return type2.Equals(PrimitiveTypes.Void) ? First.Type : type2;
            }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return First.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return First.GetCode().AddCodeBuilder(Second.GetCode());
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return First.GetDependencies().MergeDependencies(Second.GetDependencies());
        }
    }
}
