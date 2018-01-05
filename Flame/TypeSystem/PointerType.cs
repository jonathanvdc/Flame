using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type for pointers or references to values.
    /// </summary>
    public sealed class PointerType : ContainerType, IEquatable<PointerType>
    {
        /// <summary>
        /// Creates a pointer type from an element type and a pointer kind.
        /// </summary>
        /// <param name="elementType">
        /// The type of element referred to by the pointer.
        /// </param>
        /// <param name="kind">
        /// The pointer's kind.
        /// </param>
        private PointerType(IType elementType, PointerKind kind)
            : base(
                elementType,
                new PointerName(elementType.Name.Qualify(), kind),
                new PointerName(elementType.FullName, kind).Qualify(),
                AttributeMap.Empty)
        {
            this.Kind = kind;
        }

        /// <summary>
        /// Gets this pointer type's kind.
        /// </summary>
        /// <returns>The pointer kind.</returns>
        public PointerKind Kind { get; private set; }

        /// <summary>
        /// Checks if this pointer type equals an other pointer type.
        /// </summary>
        /// <param name="other">The other pointer type.</param>
        /// <returns>
        /// <c>true</c> if the pointer types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(PointerType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(ElementType, other.ElementType)
                    && Kind.Equals(other.Kind));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is PointerType && Equals((PointerType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ((object)ElementType).GetHashCode() << 2 ^ Kind.GetHashCode();
        }

        /// <inheritdoc/>
        public override ContainerType WithElementType(IType newElementType)
        {
            if (object.ReferenceEquals(ElementType, newElementType))
            {
                return this;
            }
            else
            {
                return newElementType.MakePointerType(Kind);
            }
        }

        private static ThreadLocal<LruCache<Tuple<IType, PointerKind>, PointerType>> pointerTypeCache
            = new ThreadLocal<LruCache<Tuple<IType, PointerKind>, PointerType>>(createPointerTypeCache);

        private static LruCache<Tuple<IType, PointerKind>, PointerType> createPointerTypeCache()
        {
            return new LruCache<Tuple<IType, PointerKind>, PointerType>(TypeExtensions.TypeCacheCapacity);
        }

        /// <summary>
        /// Creates a pointer type of a particular kind that has a
        /// type as element.
        /// </summary>
        /// <param name="type">
        /// The type of values referred to by the pointer type.
        /// </param>
        /// <param name="kind">
        /// The kind of the pointer type.
        /// </param>
        /// <returns>A pointer type.</returns>
        internal static PointerType Create(IType type, PointerKind kind)
        {
            return pointerTypeCache.Value.Get(
                new Tuple<IType, PointerKind>(type, kind),
                CreateImpl);
        }

        private static PointerType CreateImpl(Tuple<IType, PointerKind> input)
        {
            return new PointerType(input.Item1, input.Item2);
        }
    }

    internal sealed class StructuralPointerTypeComparer : IEqualityComparer<PointerType>
    {
        public bool Equals(PointerType x, PointerType y)
        {
            return object.ReferenceEquals(x, y)
                || (object.Equals(x.ElementType, y.ElementType)
                    && x.Kind.Equals(y.Kind));
        }

        public int GetHashCode(PointerType obj)
        {
            return (((object)obj.ElementType).GetHashCode() << 3)
                ^ obj.Kind.GetHashCode();
        }
    }

    /// <summary>
    /// Identifies a particular kind of pointer.
    /// </summary>
    public abstract class PointerKind : IEquatable<PointerKind>
    {
        /// <summary>
        /// The pointer kind for transient pointers.
        /// </summary>
        public static readonly PointerKind Transient
            = new PrimitivePointerKind("transient");

        /// <summary>
        /// The pointer kind for reference pointers, which are
        /// pointers to a value that may or may not be tracked
        /// by the garbage collection runtime.
        ///
        /// As a rule, reference pointers should never be used
        /// as the type of a field or as the element type of a
        /// container.
        /// </summary>
        public static readonly PointerKind Reference
            = new PrimitivePointerKind("ref");

        /// <summary>
        /// The pointer kind for box pointers. This kind of
        /// pointers is used both for boxed 'struct' values
        /// and for references to 'class' values.
        /// </summary>
        public static readonly PointerKind Box
            = new PrimitivePointerKind("box");

        /// <summary>
        /// Checks if this pointer kind equals another pointer kind.
        /// </summary>
        /// <param name="other">The other pointer kind.</param>
        /// <returns></returns>
        public abstract bool Equals(PointerKind other);

        /// <inheritdoc/>
        public abstract override int GetHashCode();

        /// <inheritdoc/>
        public abstract override string ToString();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is PointerKind && Equals((PointerKind)obj);
        }
    }

    internal sealed class PrimitivePointerKind : PointerKind
    {
        public PrimitivePointerKind(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(PointerKind other)
        {
            return object.ReferenceEquals(this, other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}