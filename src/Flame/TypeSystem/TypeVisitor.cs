using System;
using System.Collections.Generic;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A type of object that recursively applies a mapping to
    /// types.
    /// </summary>
    public abstract class TypeVisitor
    {
        /// <summary>
        /// Tells if a type is of interest to this visitor.
        /// Visitors always specify custom behavior for interesting
        /// types, whereas uninteresting composite types are usually
        /// treated the same: the visitor simply visits the types
        /// they are composed of.
        /// </summary>
        /// <param name="type">A type.</param>
        /// <returns>
        /// <c>true</c> if the type is interesting; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool IsOfInterest(IType type);

        /// <summary>
        /// Visits a type that has been marked as interesting.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        protected abstract IType VisitInteresting(IType type);

        /// <summary>
        /// Visits a type that has not been marked as interesting.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        protected virtual IType VisitUninteresting(IType type)
        {
            if (type is ContainerType)
            {
                var container = (ContainerType)type;
                return container.WithElementType(Visit(container.ElementType));
            }
            else if (type.IsRecursiveGenericInstance())
            {
                return Visit(type.GetRecursiveGenericDeclaration())
                    .MakeRecursiveGenericType(
                        VisitAll(type.GetRecursiveGenericArguments()));
            }
            else
            {
                return type;
            }
        }

        /// <summary>
        /// Visits a type.
        /// </summary>
        /// <param name="type">A type to visit.</param>
        /// <returns>A visited type.</returns>
        public IType Visit(IType type)
        {
            if (IsOfInterest(type))
            {
                return VisitInteresting(type);
            }
            else
            {
                return VisitUninteresting(type);
            }
        }

        /// <summary>
        /// Visits all types in a list of types.
        /// </summary>
        /// <param name="types">A list of types to visit.</param>
        /// <returns>A list of visited types.</returns>
        public IReadOnlyList<IType> VisitAll(IReadOnlyList<IType> types)
        {
            var results = new IType[types.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Visit(types[i]);
            }
            return results;
        }

        /// <summary>
        /// Visits a parameter's type and uses the result
        /// to create a new parameter.
        /// </summary>
        /// <param name="parameter">The parameter to visit.</param>
        /// <returns>A visited parameter.</returns>
        public Parameter Visit(Parameter parameter)
        {
            return parameter.WithType(Visit(parameter.Type));
        }

        /// <summary>
        /// Visits all parameters in a list of parameters.
        /// </summary>
        /// <param name="parameters">A list of parameters to visit.</param>
        /// <returns>A list of visited parameters.</returns>
        public IReadOnlyList<Parameter> VisitAll(IReadOnlyList<Parameter> parameters)
        {
            var results = new Parameter[parameters.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Visit(parameters[i]);
            }
            return results;
        }

        /// <summary>
        /// Visits a particular method's recursive generic arguments
        /// and creates a new specialization based on the results.
        /// </summary>
        /// <param name="method">The method to visit.</param>
        /// <returns>A visited method.</returns>
        public IMethod Visit(IMethod method)
        {
            if (method is DirectMethodSpecialization)
            {
                var specialization = (DirectMethodSpecialization)method;
                return Visit(specialization.Declaration)
                    .MakeGenericMethod(
                        VisitAll(specialization.GenericArguments));
            }

            var parentType = method.ParentType;
            var newParentType = Visit(parentType);
            if (!object.Equals(
                newParentType.GetRecursiveGenericDeclaration(),
                parentType.GetRecursiveGenericDeclaration()))
            {
                throw new InvalidOperationException(
                    "Cannot replace parent type of method '" + method.FullName.ToString() +
                    "' by unrelated type '" + newParentType.FullName.ToString() + "'.");
            }

            if (newParentType is TypeSpecialization)
            {
                return IndirectMethodSpecialization.Create(
                    method.GetRecursiveGenericDeclaration(),
                    (TypeSpecialization)newParentType);
            }
            else
            {
                return method.GetRecursiveGenericDeclaration();
            }
        }

        /// <summary>
        /// Visits all methods in a list of methods.
        /// </summary>
        /// <param name="methods">A list of methods to visit.</param>
        /// <returns>A list of visited methods.</returns>
        public IReadOnlyList<IMethod> VisitAll(IReadOnlyList<IMethod> methods)
        {
            var results = new IMethod[methods.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Visit(methods[i]);
            }
            return results;
        }

        /// <summary>
        /// Visits a particular field's recursive generic arguments
        /// and creates a new specialization based on the results.
        /// </summary>
        /// <param name="field">The field to visit.</param>
        /// <returns>A visited field.</returns>
        public IField Visit(IField field)
        {
            var parentType = field.ParentType;
            var newParentType = Visit(parentType);
            if (!object.Equals(
                newParentType.GetRecursiveGenericDeclaration(),
                parentType.GetRecursiveGenericDeclaration()))
            {
                throw new InvalidOperationException(
                    "Cannot replace parent type of field '" + field.FullName.ToString() +
                    "' by unrelated type '" + newParentType.FullName.ToString() + "'.");
            }

            if (newParentType is TypeSpecialization)
            {
                return IndirectFieldSpecialization.Create(
                    field.GetRecursiveGenericDeclaration(),
                    (TypeSpecialization)newParentType);
            }
            else
            {
                return field.GetRecursiveGenericDeclaration();
            }
        }

    }
}
