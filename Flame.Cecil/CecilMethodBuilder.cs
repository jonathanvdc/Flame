using Flame.Build;
using Flame.Cecil.Emit;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        public CecilMethodBuilder(ICecilType DeclaringType, MethodDefinition Method, IMethodSignatureTemplate Template)
            : base(DeclaringType, Method)
        {
            this.Template = new MethodSignatureInstance(Template, this);
        }

        public MethodSignatureInstance Template { get; private set; }

        #region Initialize

        public virtual void Initialize()
        {
            var methodDef = GetResolvedMethod();

            var attrs = methodDef.Attributes | ExtractMethodAttributes(Template.Attributes.Value);

            if (Template.Template.IsStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else if (IsAbstract)
            {
                attrs &= ~MethodAttributes.Final;
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual;
            }

            methodDef.Attributes = attrs;

            var cecilGenericParams = CecilGenericParameter.DeclareGenericParameters(methodDef, Template.GenericParameters.Value.ToArray(), Module);

            var genericParams = cecilGenericParams.Select(item => new CecilGenericParameter(item, Module, this)).ToArray();
            var conv = new GenericParameterTransformer(genericParams);
            if (!methodDef.IsConstructor)
            {
                methodDef.ReturnType = conv.Convert(Template.ReturnType.Value).GetImportedReference(Module, methodDef);
            }
            else
            {
                methodDef.ReturnType = Module.Module.TypeSystem.Void;
            }

            foreach (var item in Template.Parameters.Value)
            {
                CecilParameter.DeclareParameter(this, new RetypedParameter(item, conv.Convert(item.ParameterType)));
            }

            CecilAttribute.DeclareAttributes(methodDef, this, Template.Attributes.Value);
            if (!Template.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.ExtensionAttribute.AttributeType) &&
                Template.Parameters.Value.Any(MemberExtensions.get_IsExtension))
            {
                CecilAttribute.DeclareAttributeOrDefault(methodDef, this, PrimitiveAttributes.Instance.ExtensionAttribute);
            }

            var methodConv = new TypeMethodConverter(conv);
            var baseMethods = Template.BaseMethods.Value.Distinct().Select(methodConv.Convert).ToArray();

            var simpleBaseMethod = baseMethods.FirstOrDefault(item => !item.DeclaringType.get_IsInterface());

            if ((attrs & MethodAttributes.Virtual) == MethodAttributes.Virtual)
            {
                if (simpleBaseMethod == null)
	            {
                    attrs |= MethodAttributes.NewSlot;
	            }
            }
            else if (baseMethods.Length > 0)
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.Final;
            }

            methodDef.Attributes = attrs;

            var log = ((INamespace)DeclaringType).GetLog();
            if (simpleBaseMethod == null)
            {
                var imported = ImportMethodOverrides(DeclaringType, Module, baseMethods, genericParams);
                foreach (var item in imported)
                {
                    methodDef.Overrides.Add(item);
                }
            }
            else
            {
                var imported = ImportMethodOverrides(DeclaringType, Module, new[] { simpleBaseMethod }, genericParams);
                methodDef.Overrides.Add(imported[0]);
            }
        }

        #endregion

        #region Properties

        public bool IsAbstract
        {
            get
            {
                return DeclaringType.get_IsInterface() || Template.Attributes.Value.HasAttribute(PrimitiveAttributes.Instance.AbstractAttribute.AttributeType);
            }
        }

        #endregion

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

        public static CecilMethodBuilder DeclareMethod(ICecilTypeBuilder DeclaringType, IMethodSignatureTemplate Template)
        {
            MethodAttributes attrs = (MethodAttributes)0;
            string methodName;
            if (Template.IsConstructor)
            {
                methodName = Template.IsStatic ? ".cctor" : ".ctor";
                attrs |= MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            }
            else
            {
                methodName = Template.Name;
            }

            var methodDef = new MethodDefinition(methodName, attrs, DeclaringType.Module.Module.TypeSystem.Void);

            DeclaringType.AddMethod(methodDef);

            return new CecilMethodBuilder(DeclaringType, methodDef, Template);
        }
        public static CecilAccessorBuilder DeclareAccessor(ICecilPropertyBuilder DeclaringProperty, AccessorType Kind, IMethodSignatureTemplate Template)
        {
            string methodName = Kind.ToString().ToLower() + "_" + DeclaringProperty.Name;
            var voidTy = ((ICecilType)DeclaringProperty.DeclaringType).Module.Module.TypeSystem.Void;

            var methodDef = new MethodDefinition(methodName, MethodAttributes.SpecialName, voidTy);

            DeclaringProperty.AddAccessor(methodDef, Kind);

            return new CecilAccessorBuilder(DeclaringProperty, methodDef, Template, Kind);
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
