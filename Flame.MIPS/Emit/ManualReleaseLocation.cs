using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ManualReleaseLocation : IStorageLocation
    {
        protected ManualReleaseLocation(ICodeGenerator CodeGenerator, IStorageLocation Location)
        {
            this.CodeGenerator = CodeGenerator;
            this.Location = Location;
        }

        public IStorageLocation Location { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return Location.Type; }
        }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return Location.EmitLoad(Target);
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            return Location.EmitStore(Target);
        }

        public IAssemblerBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }

        public static ManualReleaseLocation Create(ICodeGenerator CodeGenerator, IStorageLocation Location)
        {
            if (Location is IRegister)
            {
                return new ManualReleaseRegister(CodeGenerator, (IRegister)Location);
            }
            else if (Location is IUnmanagedStorageLocation)
            {
                return new UnmanagedManualReleaseLocation(CodeGenerator, (IUnmanagedStorageLocation)Location);
            }
            else
            {
                return new ManualReleaseLocation(CodeGenerator, Location);
            }
        }
    }

    public class ManualReleaseRegister : ManualReleaseLocation, IRegister, IConstantStorage
    {
        public ManualReleaseRegister(ICodeGenerator CodeGenerator, IRegister Register)
            : base(CodeGenerator, Register)
        { }

        public IRegister Register
        {
            get
            {
                return (IRegister)Location;
            }
        }

        public string Identifier
        {
            get { return Register.Identifier; }
        }

        public int Index
        {
            get { return Register.Index; }
        }

        public RegisterType RegisterType
        {
            get { return Register.RegisterType; }
        }

        public bool IsTemporary
        {
            get { return false; }
        }

        public override string ToString()
        {
            return Identifier;
        }

        public bool IsMutable
        {
            get { return RegisterType != Emit.RegisterType.Zero; }
        }
    }

    public class UnmanagedManualReleaseLocation : ManualReleaseLocation, IUnmanagedStorageLocation
    {
        public UnmanagedManualReleaseLocation(ICodeGenerator CodeGenerator, IUnmanagedStorageLocation UnmanagedLocation)
            : base(CodeGenerator, UnmanagedLocation)
        { }

        public IUnmanagedStorageLocation UnmanagedLocation
        {
            get
            {
                return (IUnmanagedStorageLocation)Location;
            }
        }

        public IAssemblerBlock EmitLoadAddress(IRegister Target)
        {
            return UnmanagedLocation.EmitLoadAddress(Target);
        }
    }
}
