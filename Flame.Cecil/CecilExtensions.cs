using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilExtensions
    {
        #region Referencing

        private static MethodReference CloneMethodWithDeclaringType(MethodDefinition methodDef, TypeReference declaringTypeRef)
        {
            if (!declaringTypeRef.IsGenericInstance || methodDef == null)
            {
                return methodDef;
            }

            var methodRef = new MethodReference(methodDef.Name, methodDef.ReturnType, declaringTypeRef)
            {
                CallingConvention = methodDef.CallingConvention,
                HasThis = methodDef.HasThis,
                ExplicitThis = methodDef.ExplicitThis
            };

            foreach (GenericParameter genParamDef in methodDef.GenericParameters)
            {
                methodRef.GenericParameters.Add(CloneGenericParameter(genParamDef, methodRef));
            }

            methodRef.ReturnType = declaringTypeRef.Module.Import(methodDef.ReturnType, methodRef);

            foreach (ParameterDefinition paramDef in methodDef.Parameters)
            {
                methodRef.Parameters.Add(new ParameterDefinition(paramDef.Name, paramDef.Attributes, declaringTypeRef.Module.Import(paramDef.ParameterType, methodRef)));
            }

            return methodRef;
        }

        public static GenericParameter CloneGenericParameter(GenericParameter Parameter, IGenericParameterProvider ParameterProvider)
        {
            var genericParam = new GenericParameter(Parameter.Name, ParameterProvider);
            genericParam.Attributes = Parameter.Attributes;
            foreach (var item in Parameter.Constraints)
            {
                genericParam.Constraints.Add(item);
            }
            return genericParam;
        }

        public static MethodReference ReferenceMethod(this TypeReference typeRef, MethodDefinition MethodDefinition)
        {
            return CloneMethodWithDeclaringType(MethodDefinition, typeRef);

            //return typeRef.Module.Import(MethodDefinition, typeRef);
        }

        public static MethodReference ReferenceMethod(this TypeReference typeRef, Func<MethodDefinition, bool> methodSelector)
        {
            return ReferenceMethod(typeRef, typeRef.Resolve().Methods.FirstOrDefault(methodSelector));
        }

        public static MethodReference ReferenceMethod(this TypeReference typeRef, string methodName)
        {
            return ReferenceMethod(typeRef, m => m.Name == methodName);
        }

        public static MethodReference ReferenceMethod(this TypeReference typeRef, string methodName, int paramCount)
        {
            return ReferenceMethod(typeRef, m => m.Name == methodName && m.Parameters.Count == paramCount);
        }

        public static MethodReference ReferenceMethod(this TypeReference typeRef, string methodName, params TypeReference[] parameterTypes)
        {
            return ReferenceMethod(typeRef, m => m.Parameters.Select(p => p.ParameterType).SequenceEqual(parameterTypes));
        }

        public static FieldReference ReferenceField(this TypeReference typeRef, FieldDefinition fieldDef)
        {
            if (!typeRef.IsGenericInstance || fieldDef == null)
            {
                return fieldDef;
            }

            return new FieldReference(fieldDef.Name, typeRef.Module.Import(fieldDef.FieldType, typeRef), typeRef);

            // return typeRef.Module.Import(fieldDef, typeRef);
        }

        public static FieldReference ReferenceField(this TypeReference typeRef, Func<FieldDefinition, bool> fieldSelector)
        {
            return typeRef.ReferenceField(typeRef.Resolve().Fields.FirstOrDefault(fieldSelector));
        }

        public static FieldReference ReferenceField(this TypeReference typeRef, string fieldName)
        {
            return ReferenceField(typeRef, f => f.Name == fieldName);
        }

        public static MethodReference ReferencePropertyGetter(this TypeReference typeRef, Func<PropertyDefinition, bool> propertySelector)
        {
            PropertyDefinition propDef = typeRef.Resolve().Properties.FirstOrDefault(propertySelector);
            if (propDef == null || propDef.GetMethod == null)
            {
                return null;
            }

            return ReferenceMethod(typeRef, propDef.GetMethod);
        }

        public static MethodReference ReferencePropertyGetter(this TypeReference typeRef, string propertyName)
        {
            return ReferencePropertyGetter(typeRef, p => p.Name == propertyName);
        }

        public static MethodReference ReferencePropertySetter(this TypeReference typeRef, Func<PropertyDefinition, bool> propertySelector)
        {
            PropertyDefinition propDef = typeRef.Resolve().Properties.FirstOrDefault(propertySelector);
            if (propDef == null || propDef.SetMethod == null)
            {
                return null;
            }

            return ReferenceMethod(typeRef, propDef.SetMethod);
        }

        public static MethodReference ReferencePropertySetter(this TypeReference typeRef, string propertyName)
        {
            return ReferencePropertySetter(typeRef, p => p.Name == propertyName);
        }

        public static MethodReference ReferenceIfGenericDeclaringType(MethodDefinition MethodDef, ICecilType DeclaringType)
        {
            if (DeclaringType.GetAllGenericArguments().Any())
            {
                return DeclaringType.GetTypeReference().ReferenceMethod(MethodDef);
            }
            else
            {
                return MethodDef;
            }
        }

        public static FieldReference ReferenceIfGenericDeclaringType(FieldDefinition FieldDef, ICecilType DeclaringType)
        {
            if (DeclaringType.GetAllGenericArguments().Any())
            {
                return DeclaringType.GetTypeReference().ReferenceField(FieldDef);
            }
            else
            {
                return FieldDef;
            }
        }

        #endregion

        #region Generics Handling

        public static MethodReference Reference(this MethodReference MethodReference, ICecilType DeclaringType)
        {
            var module = DeclaringType.GetModule();
            MethodReference methodRef;
            if (MethodReference.Module.Name == module.Name)
            {
                methodRef = MethodReference;
            }
            else
            {
                methodRef = module.Import(MethodReference);
            }

            if (DeclaringType.IsContainerType)
            {
                return methodRef;
            }

            if (methodRef.IsDefinition)
            {
                return ReferenceIfGenericDeclaringType(methodRef.Resolve(), DeclaringType);
            }
            else
            {
                methodRef.DeclaringType = DeclaringType.GetTypeReference();
                return methodRef;
            }
        }

        public static FieldReference Reference(this FieldReference FieldReference, ICecilType DeclaringType)
        {
            var module = DeclaringType.GetModule();
            FieldReference fieldRef;
            if (FieldReference.Module.Name == module.Name)
            {
                fieldRef = FieldReference;
            }
            else
            {
                fieldRef = module.Import(FieldReference);
            }

            if (DeclaringType.IsContainerType)
            {
                return fieldRef;
            }

            if (fieldRef.IsDefinition)
            {
                return ReferenceIfGenericDeclaringType(fieldRef.Resolve(), DeclaringType);
            }
            else
            {
                fieldRef.DeclaringType = DeclaringType.GetTypeReference();
                return fieldRef;
            }
        }

        #endregion

        #region Resolve

        /*public static MethodDefinition ResolveGeneric(this MethodReference Reference)
        {
            var elemMethod = Reference.GetElementMethod();
            var resolved = elemMethod.Resolve();
            if (resolved != null)
            {
                return resolved;
            }
            var genDecl = elemMethod.DeclaringType.Resolve();
            foreach (var item in genDecl.Methods)
            {
                if (Reference.Name == item.Name && item.ReturnType.Equals(Reference.ReturnType))
                {
                    
                }
            }
        }*/

        #endregion

        public static INamespace GetDeclaringNamespace(this TypeReference Reference, CecilModule Module)
        {
            if (Reference.DeclaringType == null)
            {
                return new CecilNamespace(Module, Reference.Namespace);
            }
            else
            {
                return (INamespace)Module.Convert(Reference.DeclaringType);
            }
        }

        public static void AddOverride(this MethodDefinition Implementation, MethodReference OverriddenReference, ICompilerLog Log)
        {
            var overridenResolved = OverriddenReference.Resolve();
            if (!overridenResolved.IsVirtual && !overridenResolved.IsAbstract)
            {
                Log.LogError(new LogEntry("Invalid method override", "Method '" + Implementation.FullName + "' overrides non-virtual method '" + OverriddenReference.FullName + "'"));
            }
            foreach (var item in Implementation.Overrides)
            {
                if (item.Resolve().Equals(overridenResolved))
                {
                    return; // Already overridden
                }
            }
            Implementation.Overrides.Add(OverriddenReference);
        }

        public static string StripCLRGenerics(string Name)
        {
            int index = Name.IndexOf('`');
            if (index > -1)
            {
                return Name.Substring(0, index);
            }
            else
            {
                return Name;
            }
        }

        public static string AppendFlameGenerics(string Name, int TypeParameterCount)
        {
            if (TypeParameterCount == 0)
            {
                return Name;
            }
            return Name + '<' + new string(',', TypeParameterCount - 1) + '>';
        }

        public static string GetFlameGenericName(string Name, int TypeParameterCount)
        {
            return AppendFlameGenerics(StripCLRGenerics(Name), TypeParameterCount);
        }

        public static bool IsDelegate(this TypeDefinition Definition)
        {
            var baseType = Definition.BaseType;
            return baseType != null && baseType.FullName == "System.MulticastDelegate";
        }
    }
}
