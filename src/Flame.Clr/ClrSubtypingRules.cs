using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// Subtyping rules for the CLR's type system.
    /// </summary>
    public sealed class ClrSubtypingRules : SubtypingRules
    {
        private ClrSubtypingRules()
        {

        }

        /// <summary>
        /// An instance of the CLR subtyping rules.
        /// </summary>
        public static readonly ClrSubtypingRules Instance =
            new ClrSubtypingRules();

        /// <inheritdoc/>
        public override ImpreciseBoolean IsSubtypeOf(IType subtype, IType supertype)
        {
            if (subtype == supertype)
            {
                return ImpreciseBoolean.True;
            }

            // TODO: refine this!
            return ImpreciseBoolean.Maybe;
        }
    }
}
