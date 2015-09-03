using Flame.Build;
using Flame.Cecil.Emit;
using Flame.Compiler;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilMethodBuilder : CecilMethod, ICecilMethodBuilder
    {
        public CecilMethodBuilder(ICecilType DeclaringType, MethodDefinition Method)
            : base(DeclaringType, Method)
        {
        }

        #region ICecilMethodBuilder Implementation

        private ICodeGenerator codeGen;
        public ICodeGenerator GetBodyGenerator()
        {
            if (codeGen == null)
            {
                codeGen = new ILCodeGenerator(this);
            }
            return codeGen;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            var resolvedMethod = GetResolvedMethod();

            var context = GetEmitContext();
            if (context == null)
            {
                throw new InvalidOperationException("Cannot set an abstract method's body.");
            }
            var cg = GetBodyGenerator();
            if (IsConstructor && DeclaringType is ICecilTypeBuilder)
            {
                foreach (var item in ((ICecilTypeBuilder)DeclaringType).CreateFieldInitStatements())
                {
                    ((ICecilBlock)item.Emit(cg)).Emit(context);
                }
            }
            ((ICecilBlock)Body).Emit(context);
            context.Flush();
            resolvedMethod.Body.OptimizeMacros();
        }

        public IEmitContext GetEmitContext()
        {
            var resolvedMethod = GetResolvedMethod();
            if (resolvedMethod.IsAbstract)
            {
                return null;
            }
            return new CecilCommandEmitContext(this, resolvedMethod.Body.GetILProcessor());
        }

        public IMethod Build()
        {
            var resolvedMethod = GetResolvedMethod();
            if (resolvedMethod.IsAbstract)
            {
                if (resolvedMethod.HasBody && resolvedMethod.Body.Instructions.Count > 0)
                {
                    throw new InvalidOperationException("Cannot build an abstract method that has a method body.");
                }
                return this;
            }

            if (!resolvedMethod.IsAbstract && !resolvedMethod.IsRuntime && !resolvedMethod.IsInternalCall)
            {
                if (!resolvedMethod.HasBody || resolvedMethod.Body.Instructions.Count == 0)
                {
                    throw new InvalidOperationException("Cannot build a non-abstract, non-runtime or non-internal call method without method body.");
                }
            }
            else
            {
                if (resolvedMethod.HasBody && resolvedMethod.Body.Instructions.Count > 0)
                {
                    throw new InvalidOperationException("Cannot build an abstract, runtime or internal call method that has a method body.");
                }
            }
            return this;
        }

        #endregion

        #region Static

        #region GetMethodOverrides

        protected static MethodReference[] ImportMethodOverrides(ICecilType DeclaringType, CecilModule Module, IMethod[] Overrides, IGenericParameter[] GenericParameters)
        {
            MethodReference[] refs = new MethodReference[Overrides.Length];
            TypeReference tRef = null;
            for (int i = 0; i < refs.Length; i++)
            {
                if (tRef == null) // Optimize the case where the method has no overrides
                {
                    tRef = DeclaringType.GetTypeReference();
                }
                refs[i] = Overrides[i].GetGenericDeclaration().GetImportedReference(Module, tRef);
            }
            return refs;
        }

        #endregion

        private static T DeclareMethod<T>(ICecilType DeclaringType, IMethod Template, Action<MethodDefinition> AddMethod, Func<MethodDefinition, T> CreateMethodBuilder)
            where T : CecilMethodBuilder
        {
            var module = DeclaringType.Module;

            var attrs = ExtractMethodAttributes(Template.Attributes);

            var baseMethods = Template.BaseMethods.Distinct().ToArray();

            var simpleBaseMethod = baseMethods.FirstOrDefault((item) => !item.DeclaringType.get_IsInterface());
            if (baseMethods.Length > 0 && ((attrs & MethodAttributes.Virtual) != MethodAttributes.Virtual))
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.Final;
            }

            if (simpleBaseMethod == null && ((attrs & MethodAttributes.Virtual) == MethodAttributes.Virtual))
            {
                attrs |= MethodAttributes.NewSlot;
            }

            if (Template.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            if (DeclaringType.get_IsInterface() || Template.get_IsAbstract())
            {
                attrs &= ~MethodAttributes.Final;
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            }

            string methodName;
            if (Template is IAccessor)
            {
                var accessorType = ((IAccessor)Template).AccessorType;
                var declProp = ((IAccessor)Template).DeclaringProperty;
                methodName = accessorType.ToString().ToLower() + "_" + declProp.Name;
                attrs |= MethodAttributes.SpecialName;
            }
            else if (Template.IsConstructor)
            {
                if (Template.IsStatic)
                {
                    methodName = ".cctor";
                }
                else
                {
                    methodName = ".ctor";
                }
                attrs |= MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            }
            else
            {
                methodName = Template.Name;
            }

            var declTypeRef = DeclaringType.GetTypeReference();
            var methodDef = new MethodDefinition(methodName, attrs, declTypeRef.Module.TypeSystem.Void);

            AddMethod(methodDef);

            var cecilGenericParams = CecilGenericParameter.DeclareGenericParameters(methodDef, Template.GenericParameters.ToArray(), module);

            var cecilMethod = CreateMethodBuilder(methodDef);
            var genericParams = cecilGenericParams.Select((item) => new CecilGenericParameter(item, cecilMethod.Module, cecilMethod)).ToArray();
            if (!methodDef.IsConstructor)
            {
                methodDef.ReturnType = DeclaringType.ResolveType(CecilTypeBuilder.GetGenericType(Template.ReturnType, genericParams)).GetImportedReference(module, methodDef);
            }

            CecilAttribute.DeclareAttributes(methodDef, cecilMethod, Template.Attributes);
            if (Template.get_IsExtension() && !cecilMethod.get_IsExtension())
            {
                CecilAttribute.DeclareAttributeOrDefault(methodDef, cecilMethod, PrimitiveAttributes.Instance.ExtensionAttribute);
            }

            IParameter[] parameters = Template.GetParameters();

            foreach (var item in parameters)
            {
                CecilParameter.DeclareParameter(cecilMethod, item);
            }

            var log = ((INamespace)DeclaringType).GetLog();
            if (simpleBaseMethod == null)
            {
                var imported = ImportMethodOverrides(DeclaringType, module, baseMethods, genericParams);
                foreach (var item in imported)
                {
                    methodDef.AddOverride(item, log);
                }
            }
            else
            {
                var imported = ImportMethodOverrides(DeclaringType, module, new[] { simpleBaseMethod }, genericParams);
                methodDef.AddOverride(imported[0], log);
            }

            return cecilMethod;
        }

        public static CecilMethodBuilder DeclareMethod(ICecilTypeBuilder DeclaringType, IMethod Template)
        {
            return DeclareMethod(DeclaringType, Template, DeclaringType.AddMethod, def => new CecilMethodBuilder(DeclaringType, def));
        }
        public static CecilAccessorBuilder DeclareAccessor(ICecilPropertyBuilder DeclaringProperty, IAccessor Template)
        {
            return DeclareMethod((ICecilType)DeclaringProperty.DeclaringType, Template, (item) =>
            {
                DeclaringProperty.AddAccessor(item, Template.AccessorType);
            }, def => new CecilAccessorBuilder(DeclaringProperty, def, Template.AccessorType));
        }

        protected static MethodAttributes ExtractMethodAttributes(IEnumerable<IAttribute> Attributes)
        {
            MethodAttributes attr = MethodAttributes.HideBySig;
            bool accessSet = false;
            foreach (var item in Attributes)
            {
                if (item.AttributeType.Equals(AccessAttribute.AccessAttributeType))
                {
                    var access = ((AccessAttribute)item).Access;
                    switch (access)
                    {
                        case AccessModifier.Protected:
                            attr |= MethodAttributes.Family;
                            break;
                        case AccessModifier.Assembly:
                            attr |= MethodAttributes.Assembly;
                            break;
                        case AccessModifier.ProtectedAndAssembly:
                            attr |= MethodAttributes.FamANDAssem;
                            break;
                        case AccessModifier.ProtectedOrAssembly:
                            attr |= MethodAttributes.FamORAssem;
                            break;
                        case AccessModifier.Private:
                            attr |= MethodAttributes.Private;
                            break;
                        default:
                            attr |= MethodAttributes.Public;
                            break;
                    }
                    accessSet = true;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.AbstractAttribute.AttributeType))
                {
                    attr |= MethodAttributes.Abstract | MethodAttributes.Virtual;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.VirtualAttribute.AttributeType))
                {
                    attr |= MethodAttributes.Virtual;
                }
            }
            if (!accessSet)
            {
                attr |= MethodAttributes.Public;
            }
            return attr;
        }

        #endregion
    }
}
