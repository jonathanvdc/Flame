using System;
using Loyc.Syntax;
using Flame.Compiler;
using Flame.Compiler.Emit;

namespace Flame.Intermediate.Emit
{
	public class EmitBasicBlock : IEmitBasicBlock
	{
		public EmitBasicBlock(UniqueTag Tag, LNode Node)
		{
			this.Tag = Tag;
			this.Node = Node;
		}

		public UniqueTag Tag { get; private set; }
		public LNode Node { get; private set; }
	}
}

