using System;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// Defines helper methods for bridging the gap between
    /// IL's implicit reference types and Flame's explicit
    /// box pointers.
    /// </summary>
    public static class TypeHelpers
    {
        /// <summary>
        /// Takes a type, examines it and boxes it if it
        /// is a raw reference type.
        /// It is appropriate to call this method on the type
        /// of a value; IL values that happen to be reference
        /// types are implicitly boxed. This function hints that
        /// this implicit boxing is to be made explicit.
        /// </summary>
        /// <param name="type">
        /// The type to box if it happens to be a reference type.
        /// </param>
        /// <returns>
        /// A box-pointer type if <paramref name="type"/> is a
        /// raw reference type; otherwise, <paramref name="type"/> itself.
        /// </returns>
        public static IType BoxIfReferenceType(IType type)
        {
            if (type.IsReferenceType())
            {
                return type.MakePointerType(PointerKind.Box);
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Replaces all raw reference types with boxed reference types.
        /// </summary>
        /// <param name="type">The type to completely box.</param>
        /// <returns>A boxed type.</returns>
        public static IType BoxReferenceTypes(IType type)
        {
            // TODO: do we really need this?

            return ReferenceTypeBoxingVisitor.Instance.Visit(type);
        }

        private sealed class ReferenceTypeBoxingVisitor : TypeVisitor
        {
            private ReferenceTypeBoxingVisitor()
            { }

            public static readonly ReferenceTypeBoxingVisitor Instance =
                new ReferenceTypeBoxingVisitor();

            protected override bool IsOfInterest(IType type)
            {
                // We obviously want to match reference types here.
                //
                // In addition, we want to guarantee idempotence,
                // so we need to match on box pointers and make sure
                // we don't accidentally box their contents twice.
                //
                // Instead of actually matching on box pointers here,
                // we just mark all pointer types as interesting and
                // then sort them out in `VisitInteresting`.

                return type is PointerType || type.IsReferenceType();
            }

            protected override IType VisitInteresting(IType type)
            {
                if (type is PointerType)
                {
                    var ptr = (PointerType)type;
                    var visitedElemType = ptr.Kind == PointerKind.Box
                        ? VisitUninteresting(ptr.ElementType)
                        : Visit(ptr.ElementType);
                    return visitedElemType.MakePointerType(ptr.Kind);
                }
                else
                {
                    return type.MakePointerType(PointerKind.Box);
                }
            }
        }
    }
}
