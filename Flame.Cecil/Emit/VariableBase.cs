using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public abstract class VariableBase : IEmitVariable
    {
        public VariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract IType Type { get; }

        /// <summary>
        /// Emits instructions that push this variable's 
        /// value onto the stack. This method should
        /// not adjust the type stack.
        /// </summary>
        /// <param name="Context"></param>
        public abstract void EmitLoad(IEmitContext Context);
        /// <summary>
        /// Emits instructions that pop a value from
        /// the stack and store it in this variable.
        /// This method must adjust the type stack appropriately.
        /// </summary>
        /// <param name="Context"></param>
        public abstract void EmitStore(IEmitContext Context, ICecilBlock Value);    

        public ICecilBlock EmitGet()
        {
            return new VariableLoadBlock(this);
        }

        public ICecilBlock EmitSet(ICecilBlock Value)
        {
            return new VariableStoreBlock(this, Value);
        }

        ICodeBlock IEmitVariable.EmitGet()
        {
            return EmitGet();
        }

        ICodeBlock IEmitVariable.EmitRelease()
        {
            // Release does nothing
            return new EmptyBlock(CodeGenerator);
        }

        ICodeBlock IEmitVariable.EmitSet(ICodeBlock Value)
        {
            return EmitSet((ICecilBlock)Value);
        }
    }

    public abstract class UnmanagedVariableBase : VariableBase, IUnmanagedEmitVariable
    {
        public UnmanagedVariableBase(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        { }

        /// <summary>
        /// Emits instructions that push this variable's
        /// address onto the stack. This method should
        /// not adjust the type stack.
        /// </summary>
        /// <param name="Context"></param>
        public abstract void EmitAddress(IEmitContext Context);     


        public ICodeBlock EmitAddressOf()
        {
            return new VariableAddressBlock(this);
        }

        ICodeBlock IUnmanagedEmitVariable.EmitAddressOf()
        {
            return EmitAddressOf();
        }
    }
}
