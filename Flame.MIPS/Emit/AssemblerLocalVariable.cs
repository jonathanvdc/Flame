using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerLocalVariable : IUnmanagedEmitVariable
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

        public ICodeBlock EmitAddressOf()
        {
            this.IsUnmanaged = true;
            return new AssemblerLocalAddressOfBlock(this);
        }

        public ICodeBlock EmitGet()
        {
            return new AssemblerLocalGetBlock(this);
        }

        public ICodeBlock EmitRelease()
        {
            return new AssemblerLocalReleaseBlock(this);
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new AssemblerLocalSetBlock(this, (IAssemblerBlock)Value);
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
