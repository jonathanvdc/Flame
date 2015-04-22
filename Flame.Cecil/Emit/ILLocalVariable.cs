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
    public class ILLocalVariable : IUnmanagedEmitVariable
    {
        public ILLocalVariable(ILCodeGenerator CodeGenerator, IType Type)
            : this(CodeGenerator, Type, null)
        {
        }
        public ILLocalVariable(ILCodeGenerator CodeGenerator, IType Type, string Name)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = new DescribedVariableMember(Name, Type);
        }
        public ILLocalVariable(ILCodeGenerator CodeGenerator, IVariableMember Member)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = Member;
        }

        public ILCodeGenerator CodeGenerator { get; private set; }
        public IVariableMember Member { get; private set; }
        public string Name { get { return Member.Name; } }
        public IType Type { get { return Member.VariableType; } }

        private IEmitLocal emitLocal;
        public IEmitLocal GetEmitLocal(IEmitContext Context)
        {
            if (emitLocal == null)
            {
                emitLocal = Context.DeclareLocal(Type);
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    emitLocal.Name = Name;
                }
            }
            return emitLocal;
        }

        public ICodeBlock EmitAddressOf()
        {
            return new LocalAddressOfBlock(this);
        }

        public ICodeBlock EmitGet()
        {
            return new LocalGetBlock(this);
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new LocalSetBlock(this, (ICecilBlock)Value);
        }

        #region Recycling

        public ICodeBlock EmitRelease()
        {
            return new LocalVariableReleaseBlock(this);
        }

        public void Release(IEmitContext Context)
        {
            if (emitLocal != null)
            {
                Context.ReleaseLocal(emitLocal);
            }
        }

        #endregion
    }
}
