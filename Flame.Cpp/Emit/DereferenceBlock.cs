using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DereferenceBlock : ICppBlock
    {
        public DereferenceBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        public IType Type
        {
            get { return Value.Type.AsContainerType().ElementType; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies; }
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
            if (Value is IPointerBlock)
            {
                return ((IPointerBlock)Value).StaticDereference().GetCode();
            }
            else
            {
                var cb = new CodeBuilder(); 
                cb.Append('*');
                if (Value is BinaryOperation)
                {
                    cb.Append(BinaryOperation.GetEnclosedCode(Value));
                }
                else
                {
                    cb.Append(Value.GetCode());
                }
                return cb;
            }
        }
    }
}
