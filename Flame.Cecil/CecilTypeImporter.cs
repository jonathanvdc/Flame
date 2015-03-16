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
        public CecilTypeImporter(ModuleDefinition Module)
        {
            this.Module = Module;
            this.Context = null;
        }
        public CecilTypeImporter(ModuleDefinition Module, IGenericParameterProvider Context)
        {
            this.Module = Module;
            this.Context = Context;
        }

        public ModuleDefinition Module { get; private set; }
        public IGenericParameterProvider Context { get; private set; }

        public static TypeReference Import(ModuleDefinition Module, IType Type)
        {
            return new CecilTypeImporter(Module).Convert(Type);
        }
        public static TypeReference Import(ModuleDefinition Module, IGenericParameterProvider Context, IType Type)
        {
            return new CecilTypeImporter(Module, Context).Convert(Type);
        }
        public static IEnumerable<TypeReference> Import(ModuleDefinition Module, IGenericParameterProvider Context, IEnumerable<IType> Types)
        {
            return new CecilTypeImporter(Module, Context).Convert(Types);
        }

        protected override TypeReference ConvertTypeDefault(IType Type)
        {
            if (Type is ICecilType)
            {
                return Module.Import(((ICecilType)Type).GetTypeReference(), Context);
            }
            else
            {
                return null; // Null is a signaling value
            }
        }

        private TypeReference MakeBitType(TypeReference Reference)
        {
            return new OptionalModifierType(Module.Import(typeof(System.Runtime.CompilerServices.IsSignUnspecifiedByte)), Reference);
        }

        protected override TypeReference ConvertPrimitiveType(IType Type)
        {
            if (Type.Equals(PrimitiveTypes.Void))
                return Module.TypeSystem.Void;
            else if (Type.Equals(PrimitiveTypes.Boolean))
                return Module.TypeSystem.Boolean;
            else if (Type.Equals(PrimitiveTypes.Int8))
                return Module.TypeSystem.SByte;
            else if (Type.Equals(PrimitiveTypes.Int16))
                return Module.TypeSystem.Int16;
            else if (Type.Equals(PrimitiveTypes.Int32))
                return Module.TypeSystem.Int32;
            else if (Type.Equals(PrimitiveTypes.Int64))
                return Module.TypeSystem.Int64;
            else if (Type.Equals(PrimitiveTypes.UInt8))
                return Module.TypeSystem.Byte;
            else if (Type.Equals(PrimitiveTypes.UInt16))
                return Module.TypeSystem.UInt16;
            else if (Type.Equals(PrimitiveTypes.UInt32))
                return Module.TypeSystem.UInt32;
            else if (Type.Equals(PrimitiveTypes.UInt64))
                return Module.TypeSystem.UInt64;
            else if (Type.Equals(PrimitiveTypes.Bit8))
                return MakeBitType(Module.TypeSystem.Byte);
            else if (Type.Equals(PrimitiveTypes.Bit16))
                return MakeBitType(Module.TypeSystem.UInt16);
            else if (Type.Equals(PrimitiveTypes.Bit32))
                return MakeBitType(Module.TypeSystem.UInt32);
            else if (Type.Equals(PrimitiveTypes.Bit64))
                return MakeBitType(Module.TypeSystem.UInt64);
            else if (Type.Equals(PrimitiveTypes.Float32))
                return Module.TypeSystem.Single;
            else if (Type.Equals(PrimitiveTypes.Float64))
                return Module.TypeSystem.Double;
            else if (Type.Equals(PrimitiveTypes.Char))
                return Module.TypeSystem.Char;
            else if (Type.Equals(PrimitiveTypes.String))
                return Module.TypeSystem.String;
            else if (Type.get_IsRootType())
                return Module.TypeSystem.Object;
            else
                return base.ConvertPrimitiveType(Type);
        }

        protected override TypeReference MakeArrayType(TypeReference ElementType, int ArrayRank)
        {
            if (ArrayRank == 1)
            {
                return new ArrayType(ElementType);
            }
            else
            {
                return new ArrayType(ElementType, ArrayRank);
            }
        }

        protected override TypeReference MakeGenericType(TypeReference GenericDeclaration, IEnumerable<TypeReference> TypeArguments)
        {
            TypeReference genericDecl;
            IEnumerable<TypeReference> genArgs;
            if (GenericDeclaration.IsGenericInstance)
            {
                var declInst = ((GenericInstanceType)GenericDeclaration);
                genArgs = declInst.GenericArguments.Concat(TypeArguments);
                genericDecl = declInst.ElementType;
            }
            else
            {
                genericDecl = GenericDeclaration;
                genArgs = TypeArguments;
            }
            var instance = new GenericInstanceType(GenericDeclaration);
            foreach (var item in genArgs)
            {
                instance.GenericArguments.Add(item);
            }
            return instance;
        }

        protected override TypeReference MakePointerType(TypeReference ElementType, PointerKind Kind)
        {
            if (Kind.Equals(PointerKind.ReferencePointer))
            {
                return new ByReferenceType(ElementType);
            }
            else
            {
                return new PointerType(ElementType);
            }
        }

        protected override TypeReference MakeVectorType(TypeReference ElementType, int[] Dimensions)
        {
            return MakeArrayType(ElementType, Dimensions.Length);
        }
    }
}
