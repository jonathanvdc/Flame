using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode
{
    /// <summary>
    /// Represents an artificial instruction that can be injected in the instruction stream with a length of 0.
    /// </summary>
    public class ArtificialStatementInstruction : InstructionBase, IEquatable<ArtificialStatementInstruction>
    {
        public ArtificialStatementInstruction(int Offset, IStatement Statement, params IInstruction[] NextInstructions)
            : this(Offset, Statement, (IEnumerable<IInstruction>)NextInstructions)
        {
        }

        public ArtificialStatementInstruction(int Offset, IStatement Statement, IEnumerable<IInstruction> NextInstructions)
            : base(Offset)
        {
            this.Statement = Statement;
            this.NextInstructions = NextInstructions.Where((item) => item != null);
        }

        public IEnumerable<IInstruction> NextInstructions { get; private set; }
        public IStatement Statement { get; private set; }

        public override int Size
        {
            get { return 0; }
        }

        public override IEnumerable<IInstruction> GetNext(IBuffer<IInstruction> Instructions)
        {
            return NextInstructions;
        }

        public override void Emit(IBlockGenerator Target)
        {
            this.Statement.Emit(Target);
        }

        public bool Equals(ArtificialStatementInstruction other)
        {
            return this.Offset == other.Offset && this.Statement.Equals(other.Statement);
        }

        public override bool Equals(IInstruction other)
        {
            if (other is ArtificialStatementInstruction)
            {
                return Equals((ArtificialStatementInstruction)other);
            }
            else
            {
                return false;
            }
        }
    }
}
