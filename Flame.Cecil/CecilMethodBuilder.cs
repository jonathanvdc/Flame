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

        private BlockBuilder bodyGenerator;

        public IBlockGenerator GetBodyGenerator()
        {
            if (bodyGenerator == null)
            {
                bodyGenerator = (BlockBuilder)new ILCodeGenerator(this).CreateBlock();
            }
            return bodyGenerator;
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
            if (bodyGenerator != null)
            {
                var context = GetEmitContext();
                if (IsConstructor && DeclaringType is ICecilTypeBuilder)
                {
                    var initBlock = bodyGenerator.CodeGenerator.CreateBlock();
                    foreach (var item in ((ICecilTypeBuilder)DeclaringType).GetFieldInitStatements())
                    {
                        item.Emit(initBlock);
                    }
                    ((ICecilBlock)initBlock).Emit(context);
                }
                bodyGenerator.Emit(context);
                context.Flush();
                resolvedMethod.Body.OptimizeMacros();
                bodyGenerator = null;
            }
            else if (IsConstructor && DeclaringType is ICecilTypeBuilder)
            {
                var bodyGen = GetBodyGenerator();
                foreach (var item in ((ICecilTypeBuilder)DeclaringType).GetFieldInitStatements())
                {
                    item.Emit(bodyGenerator);
                }
                var context = GetEmitContext();
                bodyGenerator.Emit(context);
                context.Flush();
                resolvedMethod.Body.OptimizeMacros();
                bodyGenerator = null;
            }
            if (!resolvedMethod.IsAbstract && !resolvedMethod.IsRuntime && !resolvedMethod.IsInternalCall)
            {
                if (!resolvedMethod.HasBody || resolvedMethod.Body.Instructions.Count == 0)
                {
                    throw new InvalidOperationException("Cannot build a non-abstract, non-runtime or non-interal call method that has no method body.");
                }
            }
            else
            {
                if (resolvedMethod.HasBody && resolvedMethod.Body.Instructions.Count > 0)
                {
                    throw new InvalidOperationException("Cannot build an abstract, runtime or interal call method that has a method body.");
                }
            }
            return this;
        }

        #endregion

        #region Static

        #region GetMethodOverrides

        protected static IEnumerable<KeyValuePair<IMethod, MethodReference>> GetMethodOverrides(ICecilType DeclaringType, CecilModule Module, IMethod Template)
        {
            var baseMethods = Template.GetBaseMethods();
            return baseMethods.Zip(baseMethods.Select((item) => item.GetImportedReference(Module, DeclaringType.GetTypeReference())), (a, b) => new KeyValuePair<IMethod, MethodReference>(a, b));
        }

        #endregion

        private static CecilMethodBuilder DeclareMethod(ICecilType DeclaringType, IMethod Template, Action<MethodDefinition> AddMethod)
        {
            var module = DeclaringType.Module;

            var attrs = ExtractMethodAttributes(Template.GetAttributes());

            var baseMethods = GetMethodOverrides(DeclaringType, module, Template).Distinct().ToArray();

            var simpleBaseMethod = baseMethods.FirstOrDefault((item) => !item.Key.DeclaringType.get_IsInterface());
            if (baseMethods.Length > 0 && ((attrs & MethodAttributes.Virtual) != MethodAttributes.Virtual))
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.Final;
            }

            if (simpleBaseMethod.Key == null && ((attrs & MethodAttributes.Virtual) == MethodAttributes.Virtual))
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

            var cecilGenericParams = CecilGenericParameter.DeclareGenericParameters(methodDef, Template.GetGenericParameters().ToArray(), module);

            var cecilMethod = new CecilMethodBuilder(DeclaringType, methodDef);
            if (!methodDef.IsConstructor)
            {
                var genericParams = cecilGenericParams.Select((item) => new CecilGenericParameter(item, cecilMethod.Module, cecilMethod)).ToArray();
                methodDef.ReturnType = DeclaringType.ResolveType(CecilTypeBuilder.GetGenericType(Template.ReturnType, genericParams)).GetImportedReference(module, methodDef);
            }

            CecilAttribute.DeclareAttributes(methodDef, cecilMethod, Template.GetAttributes());
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
            if (simpleBaseMethod.Key == null)
            {
                foreach (var item in baseMethods)
                {
                    methodDef.AddOverride(item.Value, log);
                }
            }
            else
            {
                methodDef.AddOverride(simpleBaseMethod.Value, log);
            }

            return cecilMethod;
        }

        public static CecilMethodBuilder DeclareMethod(ICecilTypeBuilder DeclaringType, IMethod Template)
        {
            var method = DeclareMethod((ICecilType)DeclaringType, Template, DeclaringType.AddMethod);
            return method;
        }
        public static CecilMethodBuilder DeclareAccessor(ICecilPropertyBuilder DeclaringProperty, IAccessor Template)
        {
            var method = DeclareMethod((ICecilType)DeclaringProperty.DeclaringType, Template, (item) =>
            {
                DeclaringProperty.AddAccessor(item, Template.AccessorType);
            });
            return method;
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
