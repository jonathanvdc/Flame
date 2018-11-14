using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Takes a Flame type and converts it to a Cecil type reference.
        /// For this to work, <paramref name="type"/> cannot reference
        /// non-Cecil types.
        /// </summary>
        /// <param name="type">
        /// The type to convert to a type reference.
        /// </param>
        /// <returns>
        /// A type reference.
        /// </returns>
        public static Mono.Cecil.TypeReference ImportReference(
            this Mono.Cecil.ModuleDefinition module,
            IType type)
        {
            if (type is ClrTypeDefinition)
            {
                var typeRef = ((ClrTypeDefinition)type).Definition;
                if (module == null)
                {
                    // The module can be null for testing purposes.
                    return typeRef;
                }
                else
                {
                    return module.ImportReference(typeRef);
                }
            }
            else if (type is PointerType)
            {
                var pointerType = (PointerType)type;
                var elemType = pointerType.ElementType;
                var elemTypeRef = module.ImportReference(elemType);
                if (pointerType.Kind == PointerKind.Reference)
                {
                    return new Mono.Cecil.ByReferenceType(elemTypeRef);
                }
                else if (pointerType.Kind == PointerKind.Box)
                {
                    if (elemType.IsReferenceType())
                    {
                        var def = module.ImportReference(elemTypeRef);
                        return module == null ? def : module.ImportReference(def);
                    }
                    else
                    {
                        return elemTypeRef.Module.TypeSystem.Object;
                    }
                }
                else
                {
                    return new Mono.Cecil.PointerType(elemTypeRef);
                }
            }
            else
            {
                // TODO: support arrays, generics.
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets a method's extended parameter list, consists of the method's
        /// parameter list and an optional 'this' parameter as a prefix.
        /// </summary>
        /// <param name="method">
        /// The method to examine.
        /// </param>
        /// <returns>
        /// A list of parameters.
        /// </returns>
        public static IReadOnlyList<Mono.Cecil.ParameterDefinition> GetExtendedParameters(
            Mono.Cecil.MethodDefinition method)
        {
            return method.HasThis
                ? new[] { method.Body.ThisParameter }.Concat(method.Parameters).ToArray()
                : method.Parameters.ToArray();
        }

        /// <summary>
        /// Takes a Flame type and converts it to a Cecil method reference.
        /// For this to work, <paramref name="method"/> cannot reference
        /// non-Cecil types or methods.
        /// </summary>
        /// <param name="method">
        /// The method to convert to a method reference.
        /// </param>
        /// <returns>
        /// A method reference.
        /// </returns>
        public static Mono.Cecil.MethodReference ImportReference(
            this Mono.Cecil.ModuleDefinition module,
            IMethod method)
        {
            if (method is ClrMethodDefinition)
            {
                var def = ((ClrMethodDefinition)method).Definition;
                return module == null ? def : module.ImportReference(def);
            }
            else
            {
                // TODO: support generics.
                throw new NotImplementedException();
            }
        }
    }
}
