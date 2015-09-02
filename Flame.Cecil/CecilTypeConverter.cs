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
        public CecilTypeConverter(CecilModule Module, bool UsePrimitives, IDictionary<TypeReference, IType> Cache)
        {
            this.Module = Module;
            this.UsePrimitives = UsePrimitives;
            this.convertedTypes = Cache;
        }

        /// <summary>
        /// Gets the module this type converter is associated with.
        /// </summary>
        public CecilModule Module { get; private set; }

        /// <summary>
        /// Gets a boolean value that indicates if this type converter converts CLR primitives to their Flame equivalents.
        /// </summary>
        public bool UsePrimitives { get; private set; }

        private IDictionary<TypeReference, IType> convertedTypes;

        private IType ConvertModifierType(IModifierType Type)
        {
            var elemType = Convert(Type.ElementType);
            if (UsePrimitives && elemType.get_IsPrimitive() && Type.ModifierType.FullName == "System.Runtime.CompilerServices.IsSignUnspecifiedByte")
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

            var resolvedDecl = Declaration.Resolve();

            var result = new CecilType(resolvedDecl, Module);
            if (resolvedDecl.IsDelegate())
            {
                return new CecilDelegateType(result);
            }
            else
            {
                return result;
            }
        }

        private IType ConvertGenericInstance(TypeReference ElementType, IEnumerable<IType> TypeArguments)
        {
            int declTypeArgCount = ElementType.DeclaringType != null ? ElementType.DeclaringType.GenericParameters.Count : 0;
            if (declTypeArgCount > 0)
            {
                var declGeneric = (GenericTypeBase)ConvertGenericInstance(ElementType.DeclaringType, TypeArguments.Take(declTypeArgCount));
                var nestedDecl = new GenericInstanceType(Convert(ElementType), declGeneric.Resolver, declGeneric);
                if (ElementType.GenericParameters.Count - declTypeArgCount > 0)
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

        private IType ConvertGenericInstanceType(Mono.Cecil.GenericInstanceType Type)
        {
            return ConvertGenericInstance(Type.ElementType, Type.GenericArguments.Select(Convert));
        }

        private IType ConvertGenericParameter(GenericParameter Type, TypeReference DeclaringType)
        {
            if (DeclaringType.IsNested)
            {
                var declGenParams = DeclaringType.DeclaringType.GenericParameters;
                if (Type.Position < declGenParams.Count)
                {
                    return ConvertGenericParameter(declGenParams[Type.Position], DeclaringType.DeclaringType);
                }
            }
            var convertedDeclType = Convert(DeclaringType);
            return new CecilGenericParameter(Type, Module, convertedDeclType);
        }

        private IType ConvertGenericParameter(GenericParameter Type)
        {
            if (Type.DeclaringType != null)
            {
                return ConvertGenericParameter(Type, Type.DeclaringType);
            }
            return new CecilGenericParameter(Type, Module);
        }

        private IType ConvertCore(TypeReference Value)
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
                return ConvertGenericInstanceType((Mono.Cecil.GenericInstanceType)Value);
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

        public IType Convert(TypeReference Reference)
        {
            if (convertedTypes.ContainsKey(Reference))
            {
                return convertedTypes[Reference];
            }
            else
            {
                var result = ConvertCore(Reference);
                convertedTypes[Reference] = result;
                return result;
            }
        }
    }
}
