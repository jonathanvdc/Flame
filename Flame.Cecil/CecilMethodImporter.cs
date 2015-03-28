using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilMethodImporter : CecilTypeMemberImporterBase<IMethod, MethodReference>
    {
        public CecilMethodImporter(CecilModule Module)
            : base(Module)
        {
        }
        public CecilMethodImporter(CecilModule Module, IGenericParameterProvider Context)
            : base(Module, Context)
        {
        }

        public static MethodReference Import(CecilModule Module, IMethod Type)
        {
            return new CecilMethodImporter(Module).Convert(Type);
        }
        public static MethodReference Import(CecilModule Module, IGenericParameterProvider Context, IMethod Type)
        {
            return new CecilMethodImporter(Module, Context).Convert(Type);
        }
        public static IEnumerable<MethodReference> Import(CecilModule Module, IGenericParameterProvider Context, IEnumerable<IMethod> Types)
        {
            return new CecilMethodImporter(Module, Context).Convert(Types);
        }

        protected override MethodReference ConvertDeclaration(IMethod Method)
        {
            return Module.Module.Import(((ICecilMethod)Method).GetMethodReference(), Context);
        }

        protected virtual MethodReference ConvertGenericInstance(IMethod Method)
        {
            var genElem = Convert(Method.GetGenericDeclaration());
            var genInst = new GenericInstanceMethod(genElem);
            foreach (var item in Method.GetGenericArguments().Select(TypeImporter.Convert))
            {
                genInst.GenericArguments.Add(item);
            }
            return genInst;
        }

        protected override MethodReference ConvertInstanceGeneric(TypeReference DeclaringType, IMethod Member)
        {
            var decl = ConvertDeclaration(Member);
            return Module.Module.Import(DeclaringType.ReferenceMethod(decl.Resolve()), DeclaringType);
        }

        protected override MethodReference ConvertPrimitive(IMethod Member)
        {
            var declType = Member.DeclaringType;
            var type = Module.ConvertStrict(ConvertType(declType));
            if (Member is IAccessor)
            {
                var declProp = ((IAccessor)Member).DeclaringProperty;
                var propType = declProp.PropertyType;
                var indexerTypes = declProp.GetIndexerParameters().GetTypes();
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
                return Convert(cecilProp.GetAccessor(((IAccessor)Member).AccessorType));
            }
            else
            {
                return Convert(type.GetMethod(Member.Name, Member.IsStatic, Member.ReturnType, Member.GetParameters().GetTypes()));
            }
        }

        public override MethodReference Convert(IMethod Value)
        {
            if (Value.get_IsGenericInstance())
            {
                return ConvertGenericInstance(Value);
            }
            else if (Value.Equals(PrimitiveMethods.Instance.Equals))
            {
                var objType = Module.Convert(Module.Module.TypeSystem.Object);
                return Convert(objType.GetMethod("Equals", false, PrimitiveTypes.Boolean, new IType[] { objType }));
            }
            else if (Value.Equals(PrimitiveMethods.Instance.GetHashCode))
            {
                var objType = Module.Convert(Module.Module.TypeSystem.Object);
                return Convert(objType.GetMethod("GetHashCode", false, PrimitiveTypes.Int32, new IType[0]));
            }
            else
            {
                return base.Convert(Value);
            }
        }
    }
}
