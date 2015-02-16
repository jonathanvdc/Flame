using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerLocalVariable : IUnmanagedVariable
    {
        public AssemblerLocalVariable(IVariableMember Member, ICodeGenerator CodeGenerator)
        {
            this.Member = Member;
            this.CodeGenerator = CodeGenerator;
            this.IsUnmanaged = false;
        }

        public IVariableMember Member { get; private set; }
        public IType Type { get { return Member.VariableType; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public bool IsUnmanaged { get; private set; }
        private IStorageLocation location;

        public IStorageLocation Bind(IAssemblerEmitContext Context)
        {
            if (location == null)
            {
                location = Context.AllocateLocal(Type);
            }
            return ManualReleaseLocation.Create(CodeGenerator, location);
        }

        public void Release(IAssemblerEmitContext Context)
        {
            if (location != null)
            {
                location.EmitRelease().Emit(Context);
                location = null;
            }
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new AssemblerLocalGetBlock(this), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new CodeBlockStatement(new AssemblerLocalReleaseBlock(this));
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new AssemblerLocalSetBlock(this, (IAssemblerBlock)Value.Emit(CodeGenerator)));
        }

        public IExpression CreateAddressOfExpression()
        {
            this.IsUnmanaged = true;
            return new CodeBlockExpression(new AssemblerLocalAddressOfBlock(this), Type);
        }
    }

    public class AssemblerLocalGetBlock : IAssemblerBlock
    {
        public AssemblerLocalGetBlock(AssemblerLocalVariable Local)
        {
            this.Local = Local;
        }

        public AssemblerLocalVariable Local { get; private set; }
        public IType Type { get { return Local.Type; } }
        public ICodeGenerator CodeGenerator { get { return Local.CodeGenerator; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return new IStorageLocation[] { Local.Bind(Context) };
        }
    }
    public class AssemblerLocalAddressOfBlock : IAssemblerBlock
    {
        public AssemblerLocalAddressOfBlock(AssemblerLocalVariable Local)
        {
            this.Local = Local;
        }

        public AssemblerLocalVariable Local { get; private set; }
        public IType Type { get { return Local.Type.MakePointerType(PointerKind.ReferencePointer); } }
        public ICodeGenerator CodeGenerator { get { return Local.CodeGenerator; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var loc = (IUnmanagedStorageLocation)Local.Bind(Context);
            var temp = Context.AllocateRegister(Type);
            loc.EmitLoadAddress(temp).Emit(Context);
            return new IStorageLocation[] { temp };
        }
    }
    public class AssemblerLocalReleaseBlock : IAssemblerBlock
    {
        public AssemblerLocalReleaseBlock(AssemblerLocalVariable Local)
        {
            this.Local = Local;
        }

        public AssemblerLocalVariable Local { get; private set; }
        public IType Type { get { return PrimitiveTypes.Void; } }
        public ICodeGenerator CodeGenerator { get { return Local.CodeGenerator; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            Local.Release(Context);
            return new IStorageLocation[0];
        }
    }
    public class AssemblerLocalSetBlock : IAssemblerBlock
    {
        public AssemblerLocalSetBlock(AssemblerLocalVariable Local, IAssemblerBlock Value)
        {
            this.Local = Local;
            this.Value = Value;
        }

        public AssemblerLocalVariable Local { get; private set; }
        public IType Type { get { return PrimitiveTypes.Void; } }
        public ICodeGenerator CodeGenerator { get { return Local.CodeGenerator; } }
        public IAssemblerBlock Value { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var location = Local.Bind(Context);
            Value.EmitStoreTo(location, Context);
            return new IStorageLocation[0];
        }
    }
}
