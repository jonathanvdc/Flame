using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for instructions that allocate storage on the heap for
    /// an object and initialize it using a constructor.
    /// </summary>
    public sealed class NewObjectPrototype : InstructionPrototype
    {
        private NewObjectPrototype(IMethod constructor)
        {
            this.Constructor = constructor;
        }

        /// <summary>
        /// Gets the constructor to initialize objects with.
        /// </summary>
        /// <returns>The constructor to use.</returns>
        public IMethod Constructor { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType
            => Constructor.ParentType.MakePointerType(PointerKind.Box);

        /// <inheritdoc/>
        public override int ParameterCount => Constructor.Parameters.Count;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return CallPrototype.CheckArgumentTypes(
                GetArgumentList(instance),
                Constructor.Parameters,
                body);
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newMethod = mapping.MapMethod(Constructor);
            if (object.ReferenceEquals(newMethod, Constructor))
            {
                return this;
            }
            else
            {
                return Create(newMethod);
            }
        }

        /// <summary>
        /// Gets the argument list in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The formal argument list.</returns>
        public ReadOnlySlice<ValueTag> GetArgumentList(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return new ReadOnlySlice<ValueTag>(instruction.Arguments);
        }

        private static readonly InterningCache<NewObjectPrototype> instanceCache
            = new InterningCache<NewObjectPrototype>(
                new StructuralNewObjectPrototypeComparer());

        /// <summary>
        /// Gets the new-object instruction prototype for a particular constructor.
        /// </summary>
        /// <param name="constructor">The constructor to initialize objects with.</param>
        /// <returns>A new-object instruction prototype.</returns>
        public static NewObjectPrototype Create(IMethod constructor)
        {
            ContractHelpers.Assert(
                constructor.IsConstructor && !constructor.IsStatic,
                "A new-object instruction prototype's constructor method " +
                "must be an instance constructor.");
            return instanceCache.Intern(new NewObjectPrototype(constructor));
        }
    }

    internal sealed class StructuralNewObjectPrototypeComparer
        : IEqualityComparer<NewObjectPrototype>
    {
        public bool Equals(NewObjectPrototype x, NewObjectPrototype y)
        {
            return object.Equals(x.Constructor, y.Constructor);
        }

        public int GetHashCode(NewObjectPrototype obj)
        {
            return obj.Constructor.GetHashCode();
        }
    }
}