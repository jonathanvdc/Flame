using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public sealed class CecilTypeImporter : TypeConverterBase<TypeReference>
    {
        public CecilTypeImporter(CecilModule Module)
        {
            this.Module = Module;
            this.Context = null;
        }
        public CecilTypeImporter(CecilModule Module, IGenericParameterProvider Context)
        {
            this.Module = Module;
            this.Context = Context;
        }

        public CecilModule Module { get; private set; }
        public IGenericParameterProvider Context { get; private set; }

        public static TypeReference Import(CecilModule Module, IType Type)
        {
            return new CecilTypeImporter(Module).Convert(Type);
        }
        public static TypeReference Import(CecilModule Module, IGenericParameterProvider Context, IType Type)
        {
            return new CecilTypeImporter(Module, Context).Convert(Type);
        }
        public static IEnumerable<TypeReference> Import(CecilModule Module, IGenericParameterProvider Context, IEnumerable<IType> Types)
        {
            return new CecilTypeImporter(Module, Context).Convert(Types);
        }

        protected override TypeReference ConvertGenericParameter(IGenericParameter Type)
        {
            if (Type is ICecilType)
            {
                var typeRef = ((ICecilType)Type).GetTypeReference();
                if (Context == null)
                {
                    return typeRef;
                }
                return Module.Module.Import(typeRef, Context);
            }
            else
            {
                return null; // Null is a signaling value
            }
        }

        protected override TypeReference ConvertTypeDefault(IType Type)
        {
            if (Type is ICecilType)
            {
                var typeRef = ((ICecilType)Type).GetTypeReference();
                return Module.Module.Import(typeRef, Context);
            }
            else if (Type.GetIsDelegate())
            {
                return Convert(Module.TypeSystem.GetCanonicalDelegate(MethodType.GetMethod(Type)));
            }
            if (Type.GetIsRootType() || Type.Equals(PrimitiveTypes.Null))
            {
                return Module.Module.TypeSystem.Object;
            }
            else
            {
                return null; // Null is a signaling value
            }
        }

        private TypeReference MakeBitType(TypeReference Reference)
        {
            return new OptionalModifierType(Module.Module.Import(typeof(System.Runtime.CompilerServices.IsSignUnspecifiedByte)), Reference);
        }

        protected override TypeReference ConvertPrimitiveType(IType Type)
        {
            var ts = Module.Module.TypeSystem;
            if (Type.Equals(PrimitiveTypes.Void))
                return ts.Void;
            else if (Type.Equals(PrimitiveTypes.Boolean))
                return ts.Boolean;
            else if (Type.Equals(PrimitiveTypes.Int8))
                return ts.SByte;
            else if (Type.Equals(PrimitiveTypes.Int16))
                return ts.Int16;
            else if (Type.Equals(PrimitiveTypes.Int32))
                return ts.Int32;
            else if (Type.Equals(PrimitiveTypes.Int64))
                return ts.Int64;
            else if (Type.Equals(PrimitiveTypes.UInt8))
                return ts.Byte;
            else if (Type.Equals(PrimitiveTypes.UInt16))
                return ts.UInt16;
            else if (Type.Equals(PrimitiveTypes.UInt32))
                return ts.UInt32;
            else if (Type.Equals(PrimitiveTypes.UInt64))
                return ts.UInt64;
            else if (Type.Equals(PrimitiveTypes.Bit8))
                return MakeBitType(ts.Byte);
            else if (Type.Equals(PrimitiveTypes.Bit16))
                return MakeBitType(ts.UInt16);
            else if (Type.Equals(PrimitiveTypes.Bit32))
                return MakeBitType(ts.UInt32);
            else if (Type.Equals(PrimitiveTypes.Bit64))
                return MakeBitType(ts.UInt64);
            else if (Type.Equals(PrimitiveTypes.Float32))
                return ts.Single;
            else if (Type.Equals(PrimitiveTypes.Float64))
                return ts.Double;
            else if (Type.Equals(PrimitiveTypes.Char))
                return ts.Char;
            else if (Type.Equals(PrimitiveTypes.String))
                return ts.String;
            else if (Type.GetIsRootType() || Type.Equals(PrimitiveTypes.Null))
                return ts.Object;
            else
                return base.ConvertPrimitiveType(Type);
        }

        protected override TypeReference MakeArrayType(TypeReference ElementType, int ArrayRank)
        {
            if (ElementType == null)
            {
                return null;
            }
            if (ArrayRank == 1)
            {
                return new Mono.Cecil.ArrayType(ElementType);
            }
            else
            {
                return new Mono.Cecil.ArrayType(ElementType, ArrayRank);
            }
        }

        protected override TypeReference MakeGenericType(TypeReference GenericDeclaration, IEnumerable<TypeReference> TypeArguments)
        {
            if (GenericDeclaration == null || TypeArguments.Any(item => item == null))
            {
                return null;
            }

            TypeReference genericDecl;
            IEnumerable<TypeReference> genArgs;
            if (GenericDeclaration.IsGenericInstance)
            {
                var declInst = ((Mono.Cecil.GenericInstanceType)GenericDeclaration);
                genArgs = declInst.GenericArguments.Concat(TypeArguments);
                genericDecl = declInst.ElementType;
            }
            else
            {
                genericDecl = GenericDeclaration;
                genArgs = TypeArguments;
            }
            var instance = new Mono.Cecil.GenericInstanceType(GenericDeclaration);
            foreach (var item in genArgs)
            {
                instance.GenericArguments.Add(item);
            }
            return instance;
        }

        protected override TypeReference ConvertNestedType(IType Type, IType DeclaringType)
        {
            if (Type is GenericInstanceType)
            {
                var genInst = (GenericInstanceType)Type;
                var decl = Convert(genInst.GetRecursiveGenericDeclaration());
                var tArgs = genInst.GetRecursiveGenericArguments().Select(Convert).ToArray();
                return MakeGenericType(decl, tArgs);
            }
            else
            {
                return base.ConvertNestedType(Type, DeclaringType);
            }
        }

        protected override TypeReference MakeGenericInstanceType(TypeReference GenericDeclaration, TypeReference GenericDeclaringTypeInstance)
        {
            return MakeGenericType(
                GenericDeclaration,
                ((Mono.Cecil.GenericInstanceType)GenericDeclaringTypeInstance).GenericArguments);
        }

        protected override TypeReference MakePointerType(TypeReference ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return new ByReferenceType(ElementType);
            }
            else if (Kind.Equals(PointerKind.BoxPointer))
            {
                // The CLR does not have a notion of "box pointers."
                // The next best thing is a value of type object.
                return Module.Module.TypeSystem.Object;
            }
            else
            {
                return new Mono.Cecil.PointerType(ElementType);
            }
        }

        protected override TypeReference MakeVectorType(TypeReference ElementType, IReadOnlyList<int> Dimensions)
        {
            return MakeArrayType(ElementType, Dimensions.Count);
        }
    }
}
