using System;
using System.Collections.Generic;
using System.Threading;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame
{
    /// <summary>
    /// A collection of extension and helper methods that simplify
    /// working with types.
    /// </summary>
    public static class TypeExtensions
    {
        private const int typeCacheCapacity = 100;

        private static ThreadLocal<LruCache<Tuple<IType, PointerKind>, PointerType>> pointerTypeCache
            = new ThreadLocal<LruCache<Tuple<IType, PointerKind>, PointerType>>(createPointerTypeCache);

        private static LruCache<Tuple<IType, PointerKind>, PointerType> createPointerTypeCache()
        {
            return new LruCache<Tuple<IType, PointerKind>, PointerType>(typeCacheCapacity);
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
        public static PointerType MakePointerType(this IType type, PointerKind kind)
        {
            return pointerTypeCache.Value.Get(
                new Tuple<IType, PointerKind>(type, kind),
                makePointerTypeImpl);
        }

        private static PointerType makePointerTypeImpl(Tuple<IType, PointerKind> input)
        {
            return new PointerType(input.Item1, input.Item2);
        }
    }
}