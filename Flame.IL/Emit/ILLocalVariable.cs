using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILLocalVariable : IUnmanagedVariable
    {
        public ILLocalVariable(ILCodeGenerator CodeGenerator, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
        }

        public ILCodeGenerator CodeGenerator { get; private set; }
        public IEmitLocal EmitLocal { get; private set; }
        public IType Type { get; private set; }

        public void Bind(ICommandEmitContext Context)
        {
            if (this.EmitLocal == null)
            {
                this.EmitLocal = Context.DeclareLocal(Type);
            }
        }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new LocalAddressOfInstruction(CodeGenerator, this), Type.MakePointerType(PointerKind.ReferencePointer));
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new LocalGetInstruction(CodeGenerator, this), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new LocalReleaseStatement(this);
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new LocalSetInstruction(CodeGenerator, this, Value.Emit(CodeGenerator)));
        }
    }

    #region LocalReleaseStatement

    public class LocalReleaseStatement : IStatement
    {
        public LocalReleaseStatement(ILLocalVariable Local)
        {
            this.Local = Local;
        }

        public ILLocalVariable Local { get; private set; }

        public void Emit(IBlockGenerator Generator)
        {
            Local.CodeGenerator.SendToPool(Local);
        }

        public bool IsEmpty
        {
            get { return false; }
        }

        public IStatement Optimize()
        {
            return this;
        }
    }

    #endregion
}
