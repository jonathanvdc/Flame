using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Flame.TypeSystem;

namespace Flame.Llvm
{
    /// <summary>
    /// A name mangler implementation that strives for compatibility with the
    /// Itanium C++ ABI name mangling scheme.
    /// </summary>
    public sealed class ItaniumMangler : NameMangler
    {
        /// <summary>
        /// Creates an Itanium C++ ABI mangler.
        /// </summary>
        /// <param name="typeSystem">The type system to use.</param>
        public ItaniumMangler(TypeEnvironment typeSystem)
        {
            builtinTypeNames = new Dictionary<IType, string>()
            {
                { typeSystem.Void, "v" },
                { typeSystem.Char, "w" },
                { typeSystem.Boolean, "b" },
                { typeSystem.Int8, "a" },
                { typeSystem.UInt8, "h" },
                { typeSystem.Int16, "s" },
                { typeSystem.UInt16, "t" },
                { typeSystem.Int32, "i" },
                { typeSystem.UInt32, "j" },
                { typeSystem.Int64, "l" },
                { typeSystem.UInt64, "m" },
                { typeSystem.Float32, "f" },
                { typeSystem.Float64, "d" }
            };
        }

        private Dictionary<IType, string> builtinTypeNames;

        private static Dictionary<PointerKind, string> pointerKindNames =
            new Dictionary<PointerKind, string>
        {
            { PointerKind.Transient, "P" },
            { PointerKind.Reference, "R" }
        };

        /// <inheritdoc/>
        public override string Mangle(IMethod method, bool mangleFullName)
        {
            return mangleFullName ? "_Z" + EncodeFunctionName(method, true) : EncodeFunctionName(method, false);
        }

        /// <inheritdoc/>
        public override string Mangle(IField field, bool mangleFullName)
        {
            return mangleFullName ? "_Z" + EncodeQualifiedName(field) : EncodeUnqualifiedName(field.Name);
        }

        /// <inheritdoc/>
        public override string Mangle(IType type, bool mangleFullName)
        {
            return EncodeTypeName(type, mangleFullName);
        }

        private string EncodeFunctionName(IMethod method, bool includeDeclaringType)
        {
            var unqualName = method is DirectMethodSpecialization
                ? EncodeGenericInstanceName(
                    EncodeUnqualifiedName(((DirectMethodSpecialization)method).Declaration.Name),
                    method.GetGenericArguments())
                : EncodeUnqualifiedName(method.Name);

            if (!includeDeclaringType)
            {
                return unqualName;
            }

            var funcName = method.ParentType == null
                ? unqualName
                : "N" + EncodeTypeName(method.ParentType) + unqualName + "E";
            return funcName + EncodeBareFunctionType(method, false);
        }

        private string EncodeBareFunctionType(
            IMethod method, bool includeReturnType)
        {
            var builder = new StringBuilder();
            foreach (var param in method.Parameters)
            {
                builder.Append(EncodeTypeName(param.Type));
            }
            if (builder.Length == 0)
            {
                // Itanium ABI says:
                //
                //     Empty parameter lists, whether declared as () or conventionally
                //     as (void), are encoded with a void parameter specifier (v).
                //
                builder.Append("v");
            }
            if (includeReturnType)
            {
                builder.Append(EncodeTypeName(method.ReturnParameter.Type));
            }
            return builder.ToString();
        }

        private string EncodeTypeName(IType type)
        {
            return EncodeTypeName(type, true);
        }

        private string EncodeTypeName(IType type, bool includeNamespace)
        {
            string result;
            if (builtinTypeNames.TryGetValue(type, out result))
            {
                return result;
            }
            else if (type is PointerType)
            {
                var ptrType = (PointerType)type;
                string prefix;
                if (!pointerKindNames.TryGetValue(ptrType.Kind, out prefix))
                {
                    // Use a vendor-specific prefix if we can't map the pointer kind
                    // to a C++ pointer kind.
                    prefix = "U" + EncodeUnqualifiedName("P" + ptrType.Kind);
                }
                return prefix + EncodeTypeName(ptrType.ElementType);
            }
            else if (type is DirectTypeSpecialization)
            {
                var spec = (DirectTypeSpecialization)type;
                return EncodeGenericInstanceName(
                    EncodeTypeName(spec.Declaration, includeNamespace),
                    type.GetGenericArguments());
            }
            else if (includeNamespace)
            {
                return EncodeQualifiedName(type);
            }
            else
            {
                return EncodeUnqualifiedName(type.Name);
            }
        }

        private string EncodeGenericInstanceName(string genericName, IEnumerable<IType> args)
        {
            var builder = new StringBuilder();
            builder.Append(genericName);
            builder.Append("I");
            foreach (var arg in args)
            {
                builder.Append(EncodeTypeName(arg));
            }
            builder.Append("E");
            return builder.ToString();
        }

        private string EncodeQualifiedName(IMember member)
        {
            var suffix = EncodeUnqualifiedName(member.Name);
            var declaringMember = GetDeclaringMemberOrNull(member);
            if (declaringMember == null)
            {
                return suffix;
            }
            else if (declaringMember is IType)
            {
                return EncodeTypeName((IType)declaringMember) + suffix;
            }
            else
            {
                return EncodeQualifiedName(declaringMember) + suffix;
            }
        }

        private static string EncodeNamespaceName(QualifiedName name)
        {
            var result = new StringBuilder();
            while (!name.IsEmpty)
            {
                result.Append(EncodeUnqualifiedName(name.Qualifier.ToString()));
                name = name.Name;
            }
            return result.ToString();
        }

        private static string EncodeUnqualifiedName(string name)
        {
            name = Sanitize(name);
            var utf8Length = UTF8Encoding.UTF8.GetByteCount(name);
            if (utf8Length == 0)
            {
                return name;
            }
            else
            {
                return utf8Length.ToString(CultureInfo.InvariantCulture) + name;
            }
        }

        private static HashSet<char> illegalChars = new HashSet<char>()
        {
            '!'
        };

        private static string Sanitize(string name)
        {
            // TODO: find a more sophisticated encoding that rules out
            // sanitized name clashes.

            var builder = new StringBuilder(name);
            // Replace illegal characters by underscores.
            for (int i = 0; i < builder.Length; i++)
            {
                if (illegalChars.Contains(builder[i]))
                {
                    builder[i] = '_';
                }
            }
            // Prepend an underscore if the name's first character is a digit
            // because leading digits are special under the Itanium mangling
            // scheme.
            if (builder.Length > 0 && char.IsDigit(builder[0]))
            {
                builder.Insert(0, '_');
            }
            return builder.ToString();
        }

        private static string EncodeUnqualifiedName(UnqualifiedName name)
        {
            if (name is SimpleName)
            {
                return EncodeUnqualifiedName(((SimpleName)name).Name);
            }
            // else if (Name is PreMangledName)
            // {
            //     return ((PreMangledName)Name).MangledName;
            // }
            else
            {
                return EncodeUnqualifiedName(name.ToString());
            }
        }

        private static IMember GetDeclaringMemberOrNull(IMember Member)
        {
            if (Member is ITypeMember)
            {
                var declType = ((ITypeMember)Member).ParentType;
                if (declType != null)
                    return declType;
            }
            if (Member is IType)
            {
                var parent = ((IType)Member).Parent;
                if (parent.IsType)
                    return parent.Type;
                else if (parent.IsMethod)
                    return parent.Method;
            }
            return null;
        }
    }
}
