using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// A type system that extracts all relevant types from a single
    /// core library.
    /// </summary>
    public sealed class CorlibTypeSystem
    {
        /// <summary>
        /// Creates a type system based on a core library assembly.
        /// </summary>
        /// <param name="corlib">
        /// The core library, which supplies the types in the type
        /// system.
        /// </param>
        public CorlibTypeSystem(IAssembly corlib)
            : this(new ReadOnlyTypeResolver(corlib))
        { }

        /// <summary>
        /// Creates a type system based on a core library type resolver.
        /// </summary>
        /// <param name="corlibTypeResolver">
        /// A type resolver for the core library, which supplies the
        /// types in the type system.
        /// </param>
        public CorlibTypeSystem(ReadOnlyTypeResolver corlibTypeResolver)
        {
            this.CorlibTypeResolver = corlibTypeResolver;
            this.arrayTypeCache = new InterningCache<ClrArrayType>(
                new RankClrArrayTypeComparer());
            this.createArrayBaseTypes = new Lazy<Func<int, IGenericParameter, IReadOnlyList<IType>>>(
                ResolveArrayBaseTypes);
        }

        /// <summary>
        /// Gets a type resolver for the core library from
        /// which types are resolved.
        /// </summary>
        /// <returns>The core library type resolver.</returns>
        public ReadOnlyTypeResolver CorlibTypeResolver { get; private set; }

        private InterningCache<ClrArrayType> arrayTypeCache;
        private Lazy<Func<int, IGenericParameter, IReadOnlyList<IType>>> createArrayBaseTypes;

        public bool TryMakeArrayType(
            IType elementType,
            int rank,
            out IType arrayType)
        {
            arrayType = GetGenericArrayType(rank)
                .MakeGenericType(elementType);
            return true;
        }

        private ClrArrayType GetGenericArrayType(int rank)
        {
            return arrayTypeCache.Intern(
                new ClrArrayType(
                    rank,
                    param => createArrayBaseTypes.Value(rank, param)));
        }

        private Func<int, IGenericParameter, IReadOnlyList<IType>> ResolveArrayBaseTypes()
        {
            var arrayClass = CorlibTypeResolver.ResolveTypes(
                new SimpleName("Array")
                .Qualify("System"))
                .FirstOrDefault();

            var listType = CorlibTypeResolver.ResolveTypes(
                new SimpleName("IList", 1)
                .Qualify("Generic")
                .Qualify("Collections")
                .Qualify("System"))
                .FirstOrDefault();

            return (rank, param) =>
            {
                if (listType == null || rank != 1)
                {
                    return new[] { arrayClass };
                }
                else
                {
                    return new[] { arrayClass, listType.MakeGenericType(param) };
                }
            };
        }
    }
}
