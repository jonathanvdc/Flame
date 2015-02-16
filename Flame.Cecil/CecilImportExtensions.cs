using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilImportExtensions
    {
        #region GetPrimitiveReference

        #region Types

        private static TypeReference CreateBitType(ModuleDefinition Module, TypeReference UnsignedType)
        {
            return new OptionalModifierType(Module.Import(typeof(System.Runtime.CompilerServices.IsSignUnspecifiedByte)), UnsignedType);
        }

        private static TypeReference GetPrimitiveReference(IType Type, ModuleDefinition Module)
        {
            if (PrimitiveTypes.Void.Equals(Type))
            {
                return Module.TypeSystem.Void;
            }
            else if (PrimitiveTypes.Int8.Equals(Type))
            {
                return Module.TypeSystem.SByte;
            }
            else if (PrimitiveTypes.Int16.Equals(Type))
            {
                return Module.TypeSystem.Int16;
            }
            else if (PrimitiveTypes.Int32.Equals(Type))
            {
                return Module.TypeSystem.Int32;
            }
            else if (PrimitiveTypes.Int64.Equals(Type))
            {
                return Module.TypeSystem.Int64;
            }
            else if (PrimitiveTypes.UInt8.Equals(Type))
            {
                return Module.TypeSystem.Byte;
            }
            else if (PrimitiveTypes.UInt16.Equals(Type))
            {
                return Module.TypeSystem.UInt16;
            }
            else if (PrimitiveTypes.UInt32.Equals(Type))
            {
                return Module.TypeSystem.UInt32;
            }
            else if (PrimitiveTypes.UInt64.Equals(Type))
            {
                return Module.TypeSystem.UInt64;
            }
            else if (PrimitiveTypes.Bit8.Equals(Type))
            {
                return CreateBitType(Module, Module.TypeSystem.Byte);
            }
            else if (PrimitiveTypes.Bit16.Equals(Type))
            {
                return CreateBitType(Module, Module.TypeSystem.UInt16);
            }
            else if (PrimitiveTypes.Bit32.Equals(Type))
            {
                return CreateBitType(Module, Module.TypeSystem.UInt32);
            }
            else if (PrimitiveTypes.Bit64.Equals(Type))
            {
                return CreateBitType(Module, Module.TypeSystem.UInt64);
            }
            else if (PrimitiveTypes.Float32.Equals(Type))
            {
                return Module.TypeSystem.Single;
            }
            else if (PrimitiveTypes.Float64.Equals(Type))
            {
                return Module.TypeSystem.Double;
            }
            else if (PrimitiveTypes.String.Equals(Type))
            {
                return Module.TypeSystem.String;
            }
            else if (PrimitiveTypes.Char.Equals(Type))
            {
                return Module.TypeSystem.Char;
            }
            else if (PrimitiveTypes.Boolean.Equals(Type))
            {
                return Module.TypeSystem.Boolean;
            }
            else if (Type.get_IsRootType())
            {
                return Module.TypeSystem.Object;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Methods

        private static MethodReference GetPrimitiveReference(IMethod Method, ModuleDefinition Module)
        {
            if (Method.DeclaringType.get_IsPrimitive())
            {
                var declType = Method.DeclaringType;
                var type = CecilTypeBase.ImportCecil(declType, Module);
                if (Method is IAccessor)
                {
                    var declProp = ((IAccessor)Method).DeclaringProperty;
                    var propType = CecilTypeBase.Import(declProp.PropertyType, Module);
                    var indexerTypes = CecilTypeBase.Import(declProp.GetIndexerParameters().GetTypes(), Module);
                    var cecilProperties = type.GetProperties();
                    var cecilProp = cecilProperties.Single((item) =>
                    {
                        if (item.IsStatic == declProp.IsStatic && ((item.get_IsIndexer() && declProp.get_IsIndexer()) || item.Name == declProp.Name) && item.PropertyType.Equals(propType))
                        {
                            var indexerParams = item.GetIndexerParameterTypes();
                            return indexerTypes.AreEqual(indexerParams);
                        }
                        return false;
                    });
                    return ((ICecilMethod)cecilProp.GetAccessor(((IAccessor)Method).AccessorType)).GetMethodReference();
                }
                else
                {
                    return ((ICecilMethod)type.GetMethod(Method.Name, Method.IsStatic, CecilTypeBase.Import(Method.ReturnType, Module), CecilTypeBase.Import(Method.GetParameters().GetTypes(), Module))).GetMethodReference();
                }
            }
            else if (Method.Equals(PrimitiveMethods.Instance.Equals))
            {
                var objType = CecilTypeBase.Import<object>(Module);
                return ((ICecilMethod)objType.GetMethod("Equals", false, PrimitiveTypes.Boolean, new IType[] { objType })).GetMethodReference();
            }
            else if (Method.Equals(PrimitiveMethods.Instance.GetHashCode))
            {
                var objType = CecilTypeBase.ImportCecil<object>(Module);
                return ((ICecilMethod)objType.GetMethod("GetHashCode", false, PrimitiveTypes.Int32, new IType[0])).GetMethodReference();
            }
            else
            {
                return null;
            }
        }

        #endregion

        #endregion

        #region GetImportedContainerReference

        private static TypeReference GetImportedContainerReference(IType Type, Func<IType, TypeReference> Import)
        {
            var container = Type.AsContainerType();
            var elemType = Import(container.GetElementType());
            if (elemType == null)
            {
                return null;
            }
            if (container.get_IsPointer())
            {
                return CecilPointerType.CreatePointerReference(elemType, container.AsPointerType().PointerKind);
            }
            else if (container.get_IsVector())
            {
                return CecilVectorType.CreateVectorReference(elemType, container.AsVectorType().GetDimensions());
            }
            else
            {
                return CecilArrayType.CreateArrayReference(elemType, container.AsArrayType().ArrayRank);
            }
        }

        #endregion

        #region ImportCecilMethod

        private static MethodReference ImportCecilMethod(ICecilMethod Method, ModuleDefinition Module, IGenericParameterProvider Context)
        {
            var methodRef = Method.GetMethodReference();
            var importedRef = Module.Import(methodRef, Context);
            if (!importedRef.IsDefinition)
            {
                if (importedRef.IsGenericInstance)
                {
                    var genInst = (GenericInstanceMethod)importedRef;
                    genInst.ElementMethod.DeclaringType = Method.DeclaringType.GetImportedReference(Module, Context);
                    var genericArgs = Method.GetCecilGenericArguments().ToArray();
                    for (int i = 0; i < genInst.GenericArguments.Count; i++)
                    {
                        genInst.GenericArguments[i] = genericArgs[i].GetImportedReference(Module, Context);
                    }
                }
                else
                {
                    importedRef.DeclaringType = Method.DeclaringType.GetImportedReference(Module, Context);
                }
            }
            return importedRef;
        }

        #endregion

        #region GetTypeReference

        public static TypeReference GetTypeReference(this IType Type, ModuleDefinition Module)
        {
            if (Type is ICecilType)
            {
                return ((ICecilType)Type).GetTypeReference();
            }
            else
            {
                if (Type.IsContainerType)
                {
                    return GetImportedContainerReference(Type, (type) => type.GetTypeReference(Module));
                }
                return GetPrimitiveReference(Type, Module);
            }
        }

        #endregion

        #region GetImportedReference

        #region Types

        public static TypeReference GetImportedReference(this IType Type, ModuleDefinition Module, IGenericParameterProvider Context)
        {
            if (Type is ICecilType)
            {
                var cecilType = (ICecilType)Type;
                var typeRef = cecilType.GetTypeReference();
                if (typeRef.IsGenericInstance)
                {
                    var genRef = (GenericInstanceType)typeRef;
                    var genDeclRef = Module.Import(genRef.ElementType, Context);
                    var genArgRefs = cecilType.GetCecilGenericArguments().Select((item) => item.GetImportedReference(Module, Context));
                    return CecilGenericType.CreateGenericInstanceReference(genDeclRef, genArgRefs);
                }
                else
                {
                    return Module.Import(typeRef, Context);
                }
            }
            else
            {
                if (Type.IsContainerType)
                {
                    return GetImportedContainerReference(Type, (type) => type.GetImportedReference(Module, Context));
                }
                return GetPrimitiveReference(Type, Module);
            }
        }

        #endregion

        #region Methods

        public static MethodReference GetImportedReference(this IMethod Method, ModuleDefinition Module, IGenericParameterProvider Context)
        {
            if (Method is ICecilMethod)
            {
                return ImportCecilMethod((ICecilMethod)Method, Module, Context);
            }
            else
            {
                return GetPrimitiveReference(Method, Module);
            }
        }

        #endregion

        #endregion
    }
}
