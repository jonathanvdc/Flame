using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class BlockAddedEventArgs : EventArgs
    {
        public BlockAddedEventArgs(ICppBlock Block)
        {
            this.Block = Block;
        }

        public ICppBlock Block { get; private set; }
    }

    public class LocalDeclaredEventArgs : EventArgs
    {
        public LocalDeclaredEventArgs(LocalDeclaration Declaration)
        {
            this.Declaration = Declaration;
        }

        public LocalDeclaration Declaration { get; private set; }
        public CppLocal Local { get { return Declaration.Local; } }
    }

    public class NotifyingBlockGenerator : CppBlockGenerator
    {
        public NotifyingBlockGenerator(CppCodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public event EventHandler<BlockAddedEventArgs> BlockAdded;
        public event EventHandler<LocalDeclaredEventArgs> LocalDeclared;

        protected override void AddBlock(ICppBlock Block)
        {
            base.AddBlock(Block);
            if (BlockAdded != null)
	        {
                BlockAdded(this, new BlockAddedEventArgs(Block));
	        }
            if (LocalDeclared != null && Block is LocalDeclarationReference)
            {
                LocalDeclared(this, new LocalDeclaredEventArgs(((LocalDeclarationReference)Block).Declaration));
            }
        }
    }
}
