using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// Specifies the alignment of the data referred to by a
    /// load or store operation.
    /// </summary>
    public struct Alignment : IEquatable<Alignment>
    {
        /// <summary>
        /// Creates a particular alignment.
        /// </summary>
        /// <param name="value">The alignment.</param>
        public Alignment(uint value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the alignment of a pointer: a factor by which the
        /// pointer is divisible.
        /// </summary>
        /// <value>A pointer's alignment.</value>
        public uint Value { get; private set; }

        /// <summary>
        /// Tells if this alignment represents a natural alignment,
        /// i.e., a pointer whose alignment matches the natural alignment
        /// of its element type.
        /// </summary>
        public bool IsNaturallyAligned => Value == 0;

        /// <summary>
        /// Tells if this alignment represents an unaligned pointer,
        /// i.e., a pointer that is byte-aligned.
        /// </summary>
        public bool IsUnaligned => Value == 1;

        /// <summary>
        /// An alignment that represents natural alignment.
        /// </summary>
        public static readonly Alignment NaturallyAligned =
            new Alignment(0);

        /// <summary>
        /// An alignment that represents byte-alignment.
        /// </summary>
        public static readonly Alignment Unaligned =
            new Alignment(1);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)Value;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Alignment && Value == ((Alignment)obj).Value;
        }

        /// <summary>
        /// Tests if this alignment is identical to another alignment.
        /// </summary>
        /// <param name="other">An alignment to compare with this one.</param>
        /// <returns><c>true</c> if the alignments are identical; otherwise, <c>false</c>.</returns>
        public bool Equals(Alignment other)
        {
            return this == other;
        }

        /// <summary>
        /// Tests if two alignments are identical.
        /// </summary>
        /// <param name="first">The first alignment.</param>
        /// <param name="second">The second alignment.</param>
        /// <returns><c>true</c> if the alignments are identical; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Alignment first, Alignment second)
        {
            return first.Value == second.Value;
        }

        /// <summary>
        /// Tests if two alignments are not identical.
        /// </summary>
        /// <param name="first">The first alignment.</param>
        /// <param name="second">The second alignment.</param>
        /// <returns><c>false</c> if the alignments are identical; otherwise, <c>true</c>.</returns>
        public static bool operator !=(Alignment first, Alignment second)
        {
            return first.Value != second.Value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return IsNaturallyAligned ? "natural" : Value.ToString();
        }
    }

    /// <summary>
    /// A prototype for load instructions that dereference pointers.
    /// </summary>
    public sealed class LoadPrototype : InstructionPrototype
    {
        private LoadPrototype(IType elementType, bool isVolatile, Alignment alignment)
        {
            this.elemType = elementType;
            this.IsVolatile = isVolatile;
            this.Alignment = alignment;
        }

        private IType elemType;

        /// <summary>
        /// Tests if instances of this load prototype are volatile operations.
        /// Volatile operations may not be reordered with regard to each other.
        /// </summary>
        /// <value><c>true</c> if this is a prototype for volatile loads; otherwise, <c>false</c>.</value>
        public bool IsVolatile { get; private set; }

        /// <summary>
        /// Gets the pointer alignment of pointers loaded by this prototype.
        /// </summary>
        /// <value>The pointer alignment of pointers loaded by this prototype.</value>
        public Alignment Alignment { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => elemType;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var ptrType = body.Implementation.GetValueType(GetPointer(instance)) as PointerType;
            if (ptrType == null)
            {
                return new string[]
                {
                    "Target of load operation must be a pointer type."
                };
            }
            else if (!ptrType.ElementType.Equals(elemType))
            {
                return new string[]
                {
                    string.Format(
                        "Pointee type '{0}' of load argument should " +
                        "have been '{1}'.",
                        ptrType.ElementType.FullName,
                        elemType.FullName)
                };
            }
            else
            {
                return EmptyArray<string>.Value;
            }
        }

        /// <summary>
        /// Gets a variant of this load prototype with a particular volatility.
        /// </summary>
        /// <param name="isVolatile">The volatility to assign to the load.</param>
        /// <returns>
        /// A load prototype that copies all properties from this one, except for
        /// its volatility, which is set to <paramref name="isVolatile"/>.
        /// </returns>
        public LoadPrototype WithVolatility(bool isVolatile)
        {
            if (IsVolatile == isVolatile)
            {
                return this;
            }
            else
            {
                return Create(elemType, isVolatile, Alignment);
            }
        }

        /// <summary>
        /// Gets a variant of this load prototype with a particular alignment.
        /// </summary>
        /// <param name="alignment">The alignment to assign to the load.</param>
        /// <returns>
        /// A load prototype that copies all properties from this one, except for
        /// its alignment, which is set to <paramref name="alignment"/>.
        /// </returns>
        public LoadPrototype WithAlignment(Alignment alignment)
        {
            if (Alignment == alignment)
            {
                return this;
            }
            else
            {
                return Create(elemType, IsVolatile, alignment);
            }
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newType = mapping.MapType(elemType);
            if (object.ReferenceEquals(newType, elemType))
            {
                return this;
            }
            else
            {
                return Create(newType, IsVolatile, Alignment);
            }
        }

        /// <summary>
        /// Gets the pointer that is loaded by an instance of this
        /// prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <returns>
        /// The pointer whose pointee is loaded.
        /// </returns>
        public ValueTag GetPointer(Instruction instance)
        {
            AssertIsPrototypeOf(instance);
            return instance.Arguments[0];
        }

        /// <summary>
        /// Creates an instance of this load prototype.
        /// </summary>
        /// <param name="pointer">
        /// A pointer to the value to load.
        /// </param>
        /// <returns>A load instruction.</returns>
        public Instruction Instantiate(ValueTag pointer)
        {
            return Instantiate(new ValueTag[] { pointer });
        }

        private static readonly InterningCache<LoadPrototype> instanceCache
            = new InterningCache<LoadPrototype>(
                new StructuralLoadPrototypeComparer());

        /// <summary>
        /// Gets or creates a load instruction prototype for a particular
        /// element type.
        /// </summary>
        /// <param name="elementType">
        /// The type of element to load from a pointer.
        /// </param>
        /// <param name="isVolatile">
        /// Tells if instances of the load prototype are volatile operations.
        /// Volatile operations may not be reordered with regard to each other.
        /// </param>
        /// <param name="alignment">
        /// The pointer alignment of pointers loaded by the prototype.
        /// </param>
        /// <returns>
        /// A load instruction prototype.
        /// </returns>
        public static LoadPrototype Create(
            IType elementType,
            bool isVolatile = false,
            Alignment alignment = default(Alignment))
        {
            return instanceCache.Intern(new LoadPrototype(elementType, isVolatile, alignment));
        }
    }

    internal sealed class StructuralLoadPrototypeComparer
        : IEqualityComparer<LoadPrototype>
    {
        public bool Equals(LoadPrototype x, LoadPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType)
                && x.IsVolatile == y.IsVolatile
                && x.Alignment == y.Alignment;
        }

        public int GetHashCode(LoadPrototype obj)
        {
            var hash = EnumerableComparer.EmptyHash;
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.ResultType.GetHashCode());
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.IsVolatile);
            hash = EnumerableComparer.FoldIntoHashCode(hash, obj.Alignment);
            return hash;
        }
    }
}
