using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILElementVariable : IUnmanagedEmitVariable
    {
        public ILElementVariable(ICodeGenerator CodeGenerator, ICecilBlock Container, ICecilBlock[] Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Container = Container;
            this.Arguments = Arguments;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Container { get; private set; }
        public ICecilBlock[] Arguments { get; private set; }

        public IType Type
        {
            get 
            {
                var typeStack = new TypeStack();
                Container.StackBehavior.Apply(typeStack);
                return typeStack.Pop();
            }
        }

        public ICodeBlock EmitAddressOf()
        {
            return new ElementAddressOfBlock(this);
        }

        public ICodeBlock EmitGet()
        {
            return new ElementGetBlock(this);
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new ElementSetBlock(this, (ICecilBlock)Value);
        }
    }
}
