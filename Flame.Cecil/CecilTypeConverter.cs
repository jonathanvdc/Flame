using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public sealed class CecilTypeConverter : IConverter<TypeReference, IType>
    {
        private CecilTypeConverter(bool UsePrimitives)
        {
            this.UsePrimitives = UsePrimitives;
        }

        static CecilTypeConverter()
        {
            CecilPrimitiveConverter = new CecilTypeConverter(false);
            DefaultConverter = new CecilTypeConverter(true);
        }

        public static CecilTypeConverter CecilPrimitiveConverter { get; private set; }
        public static CecilTypeConverter DefaultConverter { get; private set; }

        /// <summary>
        /// Gets a boolean value that indicates if this type converter converts CLR primitives to their Flame equivalents.
        /// </summary>
        public bool UsePrimitives { get; private set; }

        private IType ConvertModifierType(IModifierType Type)
        {
            var elemType = Convert(Type.ElementType);
            if (elemType.get_IsPrimitive() && Type.ModifierType.FullName == "System.Runtime.CompilerServices.IsSignUnspecifiedByte")
            {
                switch (elemType.GetPrimitiveMagnitude())
                {
                    case 1:
                        return PrimitiveTypes.Bit8;
                    case 2:
                        return PrimitiveTypes.Bit16;
                    case 3:
                        return PrimitiveTypes.Bit32;
                    case 4:
                        return PrimitiveTypes.Bit64;
                    default:
                        break;
                }
            }
            return elemType;
        }

        private IType ConvertArrayType(ArrayType Type)
        {
            return Convert(Type.ElementType).MakeArrayType(Type.Rank);
        }

        private IType ConvertPointerType(PointerType Type)
        {
            return Convert(Type.ElementType).MakePointerType(PointerKind.TransientPointer);
        }

        private IType ConvertByReferenceType(ByReferenceType Type)
        {
            return Convert(Type.ElementType).MakePointerType(PointerKind.ReferencePointer);
        }

        private IType ConvertTypeDeclaration(TypeReference Declaration)
        {
            if (UsePrimitives)
            {
                var typeSys = Declaration.Module.TypeSystem;
                if (Declaration.Equals(typeSys.Void))
                    return PrimitiveTypes.Void;
                else if (Declaration.Equals(typeSys.SByte))
                    return PrimitiveTypes.Int8;
                else if (Declaration.Equals(typeSys.Int16))
                    return PrimitiveTypes.Int16;
                else if (Declaration.Equals(typeSys.Int32))
                    return PrimitiveTypes.Int32;
                else if (Declaration.Equals(typeSys.Int64))
                    return PrimitiveTypes.Int64;
                else if (Declaration.Equals(typeSys.Byte))
                    return PrimitiveTypes.UInt8;
                else if (Declaration.Equals(typeSys.UInt16))
                    return PrimitiveTypes.UInt16;
                else if (Declaration.Equals(typeSys.UInt32))
                    return PrimitiveTypes.UInt32;
                else if (Declaration.Equals(typeSys.UInt64))
                    return PrimitiveTypes.UInt64;
                else if (Declaration.Equals(typeSys.Single))
                    return PrimitiveTypes.Float32;
                else if (Declaration.Equals(typeSys.Double))
                    return PrimitiveTypes.Float64;
                else if (Declaration.Equals(typeSys.Boolean))
                    return PrimitiveTypes.Boolean;
                else if (Declaration.Equals(typeSys.Char))
                    return PrimitiveTypes.Char;
                else if (Declaration.Equals(typeSys.String))
                    return PrimitiveTypes.String;
            }
            return new CecilType(Declaration);
        }

        private IType ConvertGenericInstance(TypeReference ElementType, IEnumerable<IType> TypeArguments)
        {
            int declTypeArgCount = ElementType.DeclaringType != null ? ElementType.DeclaringType.GenericParameters.Count : 0;
            if (declTypeArgCount > 0)
            {
                var declGeneric = ConvertGenericInstance(ElementType.DeclaringType, TypeArguments.Take(declTypeArgCount));
                var nestedDecl = new CecilType(ElementType);
                if (ElementType.HasGenericParameters)
                {
                    return nestedDecl.MakeGenericType(TypeArguments.Skip(declTypeArgCount));
                }
                else
                {
                    return nestedDecl;
                }
            }
            else
            {
                return Convert(ElementType).MakeGenericType(TypeArguments.ToArray());
            }
        }

        private IType ConvertGenericInstanceType(GenericInstanceType Type)
        {
            return ConvertGenericInstance(Type.ElementType, Type.GenericArguments.Select(Convert));
        }

        private IType ConvertGenericParameter(GenericParameter Type)
        {
            return new CecilGenericParameter(Type);
        }

        public IType Convert(TypeReference Value)
        {
            if (Value.IsPointer)
            {
                return ConvertPointerType((PointerType)Value);
            }
            else if (Value.IsByReference)
            {
                return ConvertByReferenceType((ByReferenceType)Value);
            }
            else if (Value.IsGenericInstance)
            {
                return ConvertGenericInstanceType((GenericInstanceType)Value);
            }
            else if (Value.IsArray)
            {
                return ConvertArrayType((ArrayType)Value);
            }
            else if (Value.IsOptionalModifier || Value.IsRequiredModifier)
            {
                return ConvertModifierType((IModifierType)Value);
            }
            else if (Value.IsGenericParameter)
            {
                return ConvertGenericParameter((GenericParameter)Value);
            }
            else
            {
                return ConvertTypeDeclaration(Value);
            }
        }
    }
}
