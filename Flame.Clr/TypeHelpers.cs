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
        /// Takes a type, examines it and unboxes it if it
        /// is a box pointer type.
        /// </summary>
        /// <param name="type">
        /// The type to unbox if it happens to be a box pointer type.
        /// </param>
        /// <returns>
        /// <paramref name="type"/>'s pointee if it is a box pointer type;
        /// otherwise, <paramref name="type"/> itself.
        /// </returns>
        public static IType UnboxIfPossible(IType type)
        {
            var box = type as PointerType;
            if (box != null && box.Kind == PointerKind.Box)
            {
                return box.ElementType;
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
                // The module can be null for testing purposes.
                return module == null ? typeRef : module.ImportReference(typeRef);
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
                        return module.ImportReference(module.TypeSystem.Object);
                    }
                }
                else
                {
                    return new Mono.Cecil.PointerType(elemTypeRef);
                }
            }

            IType elementType;
            if (ClrArrayType.TryGetArrayElementType(type, out elementType))
            {
                // Handle arrays.
                int rank;
                ClrArrayType.TryGetArrayRank(type, out rank);
                return new Mono.Cecil.ArrayType(module.ImportReference(elementType), rank);
            }
            else if (type is DirectTypeSpecialization)
            {
                // Handle generics.
                var instance = new Mono.Cecil.GenericInstanceType(
                    module.ImportReference(
                        type.GetRecursiveGenericDeclaration()));

                foreach (var item in type.GetRecursiveGenericArguments())
                {
                    instance.GenericArguments.Add(module.ImportReference(item));
                }

                return instance;
            }
            else
            {
                throw new NotSupportedException($"Cannot import ill-understood type '{type.FullName}'.");
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
        /// Takes a Flame method and converts it to a Cecil method reference.
        /// For this to work, <paramref name="field"/> cannot reference
        /// non-Cecil types or methods.
        /// </summary>
        /// <param name="field">
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
            else if (method is IndirectMethodSpecialization)
            {
                var specialization = (IndirectMethodSpecialization)method;
                return CloneMethodWithDeclaringType(
                    module.ImportReference(specialization.Declaration),
                    module.ImportReference(specialization.ParentType));
            }
            else if (method is DirectMethodSpecialization)
            {
                var specialization = (DirectMethodSpecialization)method;
                var genInst = new Mono.Cecil.GenericInstanceMethod(
                    module.ImportReference(specialization.Declaration));

                foreach (var item in specialization.GenericArguments)
                {
                    genInst.GenericArguments.Add(module.ImportReference(item));
                }
                return genInst;
            }
            else
            {
                throw new NotSupportedException($"Cannot import ill-understood method '{method.FullName}'.");
            }
        }

        /// <summary>
        /// Takes a Flame field and converts it to a Cecil field reference.
        /// For this to work, <paramref name="field"/> cannot reference
        /// non-Cecil types or methods.
        /// </summary>
        /// <param name="field">
        /// The field to convert to a field reference.
        /// </param>
        /// <returns>
        /// A field reference.
        /// </returns>
        public static Mono.Cecil.FieldReference ImportReference(
            this Mono.Cecil.ModuleDefinition module,
            IField field)
        {
            if (field is ClrFieldDefinition)
            {
                var def = ((ClrFieldDefinition)field).Definition;
                return module == null ? def : module.ImportReference(def);
            }
            else if (field is IndirectFieldSpecialization)
            {
                var specialization = (IndirectFieldSpecialization)field;
                var declarationRef = module.ImportReference(specialization.Declaration);
                var typeRef = module.ImportReference(specialization.ParentType);
                return new Mono.Cecil.FieldReference(
                    declarationRef.Name,
                    module.ImportReference(declarationRef.FieldType, typeRef), typeRef);
            }
            else
            {
                throw new NotSupportedException($"Cannot import ill-understood field '{field.FullName}'.");
            }
        }

        private static Mono.Cecil.MethodReference CloneMethodWithDeclaringType(
            Mono.Cecil.MethodReference methodDef,
            Mono.Cecil.TypeReference declaringTypeRef)
        {
            if (!declaringTypeRef.IsGenericInstance || methodDef == null)
            {
                return methodDef;
            }

            var methodRef = new Mono.Cecil.MethodReference(methodDef.Name, methodDef.ReturnType, declaringTypeRef)
            {
                CallingConvention = methodDef.CallingConvention,
                HasThis = methodDef.HasThis,
                ExplicitThis = methodDef.ExplicitThis
            };

            foreach (Mono.Cecil.GenericParameter genParamDef in methodDef.GenericParameters)
            {
                methodRef.GenericParameters.Add(CloneGenericParameter(genParamDef, methodRef));
            }

            methodRef.ReturnType = declaringTypeRef.Module.ImportReference(methodDef.ReturnType, methodRef);

            foreach (Mono.Cecil.ParameterDefinition paramDef in methodDef.Parameters)
            {
                methodRef.Parameters.Add(
                    new Mono.Cecil.ParameterDefinition(
                        paramDef.Name, paramDef.Attributes,
                        declaringTypeRef.Module.ImportReference(paramDef.ParameterType, methodRef)));
            }

            return methodRef;
        }

        public static Mono.Cecil.GenericParameter CloneGenericParameter(
            Mono.Cecil.GenericParameter Parameter,
            Mono.Cecil.IGenericParameterProvider ParameterProvider)
        {
            var genericParam = new Mono.Cecil.GenericParameter(Parameter.Name, ParameterProvider);
            genericParam.Attributes = Parameter.Attributes;
            foreach (var item in Parameter.Constraints)
            {
                genericParam.Constraints.Add(item);
            }
            return genericParam;
        }
    }
}
