using System;
using System.Collections.Generic;

namespace Flame
{
    /// <summary>
    /// Defines a type of value.
    /// </summary>
    public interface IType : IMember, IGenericMember
    {
        /// <summary>
        /// Gets the parent entity that defines and owns this type.
        /// </summary>
        /// <returns>The parent entity.</returns>
        TypeParent Parent { get; }

        /// <summary>
        /// Gets this type's base types. Base types can be either classes or
        /// interfaces.
        /// </summary>
        /// <returns>A read-only list of base types.</returns>
        IReadOnlyList<IType> BaseTypes { get; }

        /// <summary>
        /// Gets this type's fields.
        /// </summary>
        /// <returns>A read-only list of fields.</returns>
        IReadOnlyList<IField> Fields { get; }

        /// <summary>
        /// Gets this type's methods.
        /// </summary>
        /// <returns>A read-only list of methods.</returns>
        IReadOnlyList<IMethod> Methods { get; }

        /// <summary>
        /// Gets this type's properties.
        /// </summary>
        /// <returns>A read-only list of properties.</returns>
        IReadOnlyList<IProperty> Properties { get; }

        /// <summary>
        /// Gets the nested types defined by this type.
        /// </summary>
        /// <returns>A read-only list of nested types.</returns>
        IReadOnlyList<IType> NestedTypes { get; }
    }

    /// <summary>
    /// Gets a type's parent, that is, the entity that defines
    /// the type. A type parent can be either an assembly, another
    /// type, a method (for generic parameters only), or nothing at all.
    /// </summary>
    public struct TypeParent
    {
        /// <summary>
        /// Creates a type parent that wraps around an assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly that defines a type.
        /// </param>
        public TypeParent(IAssembly assembly)
        {
            this = default(TypeParent);
            this.AssemblyOrNull = assembly;
        }

        /// <summary>
        /// Creates a type parent that wraps around a type.
        /// </summary>
        /// <param name="type">
        /// The type that defines another type.
        /// </param>
        public TypeParent(IType type)
        {
            this = default(TypeParent);
            this.TypeOrNull = type;
        }

        /// <summary>
        /// Creates a type parent that wraps around a method.
        /// </summary>
        /// <param name="method">
        /// The method that defines a type.
        /// </param>
        public TypeParent(IMethod method)
        {
            this = default(TypeParent);
            this.MethodOrNull = method;
        }

        /// <summary>
        /// Gets a type parent that indicates that a type has no parent.
        /// </summary>
        public static TypeParent Nothing => default(TypeParent);

        /// <summary>
        /// Gets the assembly that is this type parent. If this type
        /// parent is not an assembly, then <c>null</c> is returned.
        /// </summary>
        /// <returns>An assembly or <c>null</c>.</returns>
        public IAssembly AssemblyOrNull { get; private set; }

        /// <summary>
        /// Gets the type that is this type parent. If this type
        /// parent is not a type, then <c>null</c> is returned.
        /// </summary>
        /// <returns>A type or <c>null</c>.</returns>
        public IType TypeOrNull { get; private set; }

        /// <summary>
        /// Gets the method that is this type parent. If this type
        /// parent is not a method, then <c>null</c> is returned.
        /// </summary>
        /// <returns>A method or <c>null</c>.</returns>
        public IMethod MethodOrNull { get; private set; }

        /// <summary>
        /// Gets the assembly, type or method that is this type parent
        /// as a member. If this type parent is nothing, then <c>null</c>
        /// is returned.
        /// </summary>
        /// <returns>A member or <c>null</c>.</returns>
        public IMember MemberOrNull =>
            TypeOrNull ?? MethodOrNull ?? (IMember)AssemblyOrNull;

        /// <summary>
        /// Checks if this type parent is an assembly.
        /// </summary>
        public bool IsAssembly => AssemblyOrNull != null;

        /// <summary>
        /// Checks if this type parent is a type.
        /// </summary>
        public bool IsType => TypeOrNull != null;

        /// <summary>
        /// Checks if this type parent is a method.
        /// </summary>
        public bool IsMethod => MethodOrNull != null;

        /// <summary>
        /// Checks if this type parent is nothing at all, that is,
        /// no entity directly "defines" the type.
        /// </summary>
        public bool IsNothing => !IsAssembly && !IsType && !IsMethod;

        /// <summary>
        /// Gets the assembly that is this type parent. Throws
        /// if this type parent is not an assembly.
        /// </summary>
        /// <returns>An assembly.</returns>
        public IAssembly Assembly
        {
            get
            {
                if (!IsAssembly)
                    throw new InvalidOperationException("Type parent is not an assembly.");

                return AssemblyOrNull;
            }
        }

        /// <summary>
        /// Gets the type that is this type parent. Throws
        /// if this type parent is not a type.
        /// </summary>
        /// <returns>A type.</returns>
        public IType Type
        {
            get
            {
                if (!IsType)
                    throw new InvalidOperationException("Type parent is not a type.");

                return TypeOrNull;
            }
        }

        /// <summary>
        /// Gets the method that is this type parent. Throws
        /// if this type parent is not a method.
        /// </summary>
        /// <returns>A method.</returns>
        public IMethod Method
        {
            get
            {
                if (!IsMethod)
                    throw new InvalidOperationException("Type parent is not a method.");

                return MethodOrNull;
            }
        }

        /// <summary>
        /// Gets the assembly, type or method that is this type parent.
        /// Throws if this type parent is not an assembly, type or method.
        /// </summary>
        /// <returns>A member.</returns>
        public IMember Member
        {
            get
            {
                var result = MemberOrNull;
                if (result == null)
                {
                    throw new InvalidOperationException(
                        "Type parent is not an assembly, type or method.");
                }

                return result;
            }
        }
    }
}