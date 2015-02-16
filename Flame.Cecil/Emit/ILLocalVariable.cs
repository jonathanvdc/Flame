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
    public class ILLocalVariable : IUnmanagedVariable
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
                    emitLocal.SetName(Name);
                }
            }
            return emitLocal;
        }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new LocalAddressOfBlock(this), Type.MakePointerType(PointerKind.ReferencePointer));
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new LocalGetBlock(this), Type);
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new LocalSetBlock(this, (ICecilBlock)Value.Emit(CodeGenerator)));
        }

        #region Recycling

        public IStatement CreateReleaseStatement()
        {
            //return new CodeBlockStatement(new LocalVariableReleaseBlock(this));
            return new ILLocalVariableReleaseStatement(this);
        }

        public void Release()
        {
            CodeGenerator.ReleaseLocal(this);
        }

        #endregion
    }
}
