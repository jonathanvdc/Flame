using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilTypeBuilder : CecilType, ICecilTypeBuilder
    {
        public CecilTypeBuilder(TypeDefinition Definition, INamespace DeclaringNamespace, CecilModule Module)
            : this(Definition, DeclaringNamespace, new EmptyGenericResolver(), Module)
        {
        }
        public CecilTypeBuilder(TypeDefinition Definition, INamespace DeclaringNamespace, IGenericResolver Resolver, CecilModule Module)
            : base(Definition, Module)
        {
            this.declNs = DeclaringNamespace;
            this.Resolver = Resolver;
        }
        public CecilTypeBuilder(CecilResolvedTypeBase Type, INamespace DeclaringNamespace, CecilModule Module)
            : this(Type.GetResolvedType(), DeclaringNamespace, Type, Module)
        {
        }

        public override IType GetGenericDeclaration()
        {
            return this;
        }

        #region Declaring Namespace

        private INamespace declNs;
        public override INamespace DeclaringNamespace
        {
            get
            {
                return declNs;
            }
        }

        #endregion

        #region Generic Parameter Resolution

        public IGenericResolver Resolver { get; private set; }
        public override IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return Resolver.ResolveTypeParameter(TypeParameter);
        }

        #endregion

        #region Static

        private static string CreateCLRName(IType Template)
        {
            if (!Template.get_IsGeneric())
            {
                return Template.Name;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Template.GetGenericFreeName());
                sb.Append('`');
                sb.Append(Template.GetGenericParameters().Count());
                return sb.ToString();
            }
        }

        public static IType[] GetGenericTypes(IType[] SourceTypes, IGenericParameter[] GenericParameters)
        {
            return new GenericParameterTransformer(GenericParameters).Convert(SourceTypes).ToArray();
        }

        public static IType GetGenericType(IType SourceType, IGenericParameter[] GenericParameters)
        {
            return new GenericParameterTransformer(GenericParameters).Convert(SourceType);
        }

        public static CecilTypeBuilder DeclareType(ICecilNamespace CecilNamespace, IType Template)
        {
            var isNested = CecilNamespace is ICecilType;
            var typeAttrs = ExtractTypeAttributes(Template.GetAttributes());
            if (isNested && (typeAttrs & TypeAttributes.Public) == TypeAttributes.Public)
            {
                typeAttrs &= ~TypeAttributes.Public;
                typeAttrs |= TypeAttributes.NestedPublic;
            }

            var reference = new TypeDefinition(CecilNamespace.FullName, CreateCLRName(Template), typeAttrs);

            CecilTypeBuilder cecilType;
            IGenericResolver genericResolver;
            bool isEnum = Template.get_IsEnum();
            //IGenericResolver resolver = isEnum || !Template.GetGenericParameters().Any() ? 
            if (isEnum)
            {
                cecilType = new CecilEnumBuilder(reference, CecilNamespace, CecilNamespace.Module);
                genericResolver = new EmptyGenericResolver();
            }
            else if (CecilNamespace is ICecilType)
            {
                var declType = (ICecilType)CecilNamespace;
                var mapResolver = new GenericResolverMap(declType);
                cecilType = new CecilTypeBuilder(reference, CecilNamespace, mapResolver, CecilNamespace.Module);
                var declGenerics = declType.GetGenericParameters().ToArray();
                var inheritedGenerics = CecilGenericParameter.DeclareGenericParameters(reference, declGenerics, cecilType.Module, cecilType);
                mapResolver.Map(declGenerics, inheritedGenerics);
                genericResolver = mapResolver;
            }
            else
            {
                genericResolver = new EmptyGenericResolver();
                cecilType = new CecilTypeBuilder(reference, CecilNamespace, genericResolver, CecilNamespace.Module);
            }

            CecilNamespace.AddType(reference);

            var module = CecilNamespace.Module.Module;

            // generics
            var genericTemplates = Template.GetGenericParameters().ToArray();

            var genericParams = CecilGenericParameter.DeclareGenericParameters(reference, genericTemplates, cecilType.Module, cecilType);

            var baseTypes = genericResolver.ResolveTypes(GetGenericTypes(Template.GetBaseTypes(), genericParams));

            if (!Template.get_IsInterface())
            {
                if (isEnum)
                {
                    reference.BaseType = module.Import(typeof(Enum));
                }
                else if (Template.get_IsValueType())
                {
                    reference.BaseType = module.Import(typeof(ValueType));
                }
                else
                {
                    var parentType = baseTypes.SingleOrDefault((item) => !item.get_IsInterface());
                    if (parentType != null)
                    {
                        reference.BaseType = parentType.GetImportedReference(CecilNamespace.Module, reference);
                    }
                    else
                    {
                        reference.BaseType = module.TypeSystem.Object;
                    }
                }
            }
            foreach (var item in baseTypes.Where((item) => item.get_IsInterface()))
            {
                reference.Interfaces.Add(item.GetImportedReference(CecilNamespace.Module, reference));
            }

            CecilAttribute.DeclareAttributes(reference, cecilType, Template.GetAttributes());

            if (Template.get_IsExtension() && !cecilType.get_IsExtension())
            {
                CecilAttribute.DeclareAttributeOrDefault(reference, cecilType, PrimitiveAttributes.Instance.ExtensionAttribute);
            }

            if (isEnum)
            {
                var field = new FieldDefinition("value__",
                    FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName,
                    baseTypes.SingleOrDefault((item) => !item.get_IsInterface()).GetImportedReference(CecilNamespace.Module, reference));
                cecilType.AddField(field);
            }

            return cecilType;
        }

        #endregion

        #region ICecilTypeBuilder Implementation

        public virtual IMethodBuilder DeclareMethod(IMethod Template)
        {
            var method = CecilMethodBuilder.DeclareMethod(this, Template);
            ClearMethodCache();
            ClearConstructorCache();
            return method;
        }

        public virtual IFieldBuilder DeclareField(IField Template)
        {
            var field = CecilField.DeclareField(this, Template);
            ClearFieldCache();
            return field;
        }

        public virtual IPropertyBuilder DeclareProperty(IProperty Template)
        {
            var property = CecilPropertyBuilder.DeclareProperty(this, Template);
            if (Template.get_IsIndexer())
            {
                this.GetResolvedType().SetDefaultMember(property.Name);
            }
            ClearPropertyCache();
            return property;
        }

        public void AddField(FieldDefinition Field)
        {
            GetResolvedType().Fields.Add(Field);
        }

        public void AddMethod(MethodDefinition Method)
        {
            GetResolvedType().Methods.Add(Method);
        }

        public void AddProperty(PropertyDefinition Property)
        {
            GetResolvedType().Properties.Add(Property);
        }

        public void AddEvent(EventDefinition Event)
        {
            GetResolvedType().Events.Add(Event);
        }

        private static IExpression CreateSingletonCall(IExpression GetSingletonExpression, IMethod Method)
        {
            var parameters = Method.GetParameters();
            var args = new IExpression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = new ArgumentVariable(parameters[i], i).CreateGetExpression();
            }
            return new InvocationExpression(Method, GetSingletonExpression, args);
        }

        private static void CreateStaticSingletonMethod(IExpression GetSingletonExpression, ITypeBuilder DeclaringType, IMethod Method)
        {
            var descMethod = new DescribedMethod(Method.Name, DeclaringType, Method.ReturnType, true);
            foreach (var attr in Method.GetAttributes())
            {
                descMethod.AddAttribute(attr);
            }
            foreach (var param in Method.GetParameters())
            {
                descMethod.AddParameter(param);
            }
            var staticMethod = DeclaringType.DeclareMethod(descMethod);
            var call = CreateSingletonCall(GetSingletonExpression, Method);
            var bodyGen = staticMethod.GetBodyGenerator();
            staticMethod.SetMethodBody(bodyGen.EmitReturn(call.Emit(bodyGen)));
            staticMethod.Build();
        }

        private static void CreateStaticSingletonAccessor(IExpression GetSingletonExpression, IPropertyBuilder DeclaringProperty, IAccessor Accessor)
        {
            var descMethod = new DescribedAccessor(Accessor.AccessorType, DeclaringProperty, Accessor.ReturnType);
            descMethod.IsStatic = true;
            foreach (var attr in Accessor.GetAttributes())
            {
                descMethod.AddAttribute(attr);
            }
            foreach (var param in Accessor.GetParameters())
            {
                descMethod.AddParameter(param);
            }
            var staticMethod = DeclaringProperty.DeclareAccessor(descMethod);
            var call = CreateSingletonCall(GetSingletonExpression, Accessor);
            var bodyGen = staticMethod.GetBodyGenerator();
            staticMethod.SetMethodBody(bodyGen.EmitReturn(call.Emit(bodyGen)));
            staticMethod.Build();
        }

        private static void CreateStaticSingletonProperty(IExpression GetSingletonExpression, ITypeBuilder DeclaringType, IProperty Property)
        {
            var descProp = new DescribedProperty(Property.Name, DeclaringType, Property.PropertyType, true);
            foreach (var attr in Property.GetAttributes())
            {
                descProp.AddAttribute(attr);
            }
            foreach (var param in Property.GetIndexerParameters())
            {
                descProp.AddIndexerParameter(param);
            }
            var staticProp = DeclaringType.DeclareProperty(descProp);
            foreach (var item in Property.GetAccessors())
            {
                CreateStaticSingletonAccessor(GetSingletonExpression, staticProp, item);
            }
            staticProp.Build();
        }

        public IType Build()
        {
            initialValues = null;
            if (this.Name == StaticSingletonName)
            {
                var declTypeBuilder = this.DeclaringGenericMember as ITypeBuilder;
                var singletonProp = this.GetProperties().GetProperty(this.GetSingletonMemberName(), true);
                // Generate static members for C# compatibility if "-generate-static" was specified
                if (declTypeBuilder != null && singletonProp != null && declTypeBuilder.GetLog().Options.GenerateStaticMembers())
                {
                    var getSingletonExpr = new PropertyVariable(singletonProp).CreateGetExpression();
                    foreach (var item in this.GetMethods())
                        if (item.get_Access() == AccessModifier.Public)
                        if (!item.IsStatic)
                    {
                        CreateStaticSingletonMethod(getSingletonExpr, declTypeBuilder, item);
                    }
                    foreach (var item in this.GetProperties())
                        if (item.get_Access() == AccessModifier.Public)
                        if (!item.IsStatic)
                    {
                        CreateStaticSingletonProperty(getSingletonExpr, declTypeBuilder, item);
                    }
                }
            }
            return this;
        }

        private Dictionary<ICecilField, IExpression> initialValues;
        public void SetInitialValue(ICecilField Field, IExpression Value)
        {
            if (initialValues == null)
            {
                initialValues = new Dictionary<ICecilField, IExpression>();
            }
            initialValues[Field] = Value;
        }

        public IList<IStatement> GetFieldInitStatements()
        {
            List<IStatement> statements = new List<IStatement>();
            if (initialValues == null)
            {
                return statements;
            }
            foreach (var item in initialValues)
            {
                var fieldType = item.Key.FieldType;
                if (item.Value != null && !(item.Value is DefaultValueExpression && ((DefaultValueExpression)item.Value).Type.Equals(fieldType)))
                {
                    bool setField = false;
                    if (item.Value.IsConstant)
                    {
                        if (fieldType.get_IsPrimitive())
                        {
                            if (!IsDefaultValue(item.Value, fieldType))
                            {
                                setField = true;
                            }
                        }
                        else
                        {
                            setField = fieldType.get_IsValueType() || !IsNullValue(item.Value);
                        }
                    }
                    else
                    {
                        setField = true;
                    }
                    if (setField)
                    {
                        statements.Add(new FieldVariable(item.Key, new ThisVariable(this).CreateGetExpression()).CreateSetStatement(item.Value));
                    }
                }
            }
            return statements;
        }

        #region IsDefaultPrimitiveValue

        private static bool IsDefaultValue(IExpression Value, IType Type)
        {
            object eval = Value.Evaluate().GetPrimitiveValue<object>();
            object def = Type.GetDefaultValue().GetPrimitiveValue<object>();
            return eval.Equals(def);
        }

        #endregion

        #region IsNullValue

        private static bool IsNullValue(IExpression Value)
        {
            /*object eval = Value.Evaluate().GetPrimitiveValue<object>();
            return eval == null;*/
            return Value is NullExpression;
        }

        #endregion

        #endregion

        #region ExtractTypeAttributes

        private static TypeAttributes ExtractTypeAttributes(IEnumerable<IAttribute> Attributes)
        {
            TypeAttributes attr = TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed;
            bool accessSet = false;
            foreach (var item in Attributes)
            {
                if (item.AttributeType.Equals(AccessAttribute.AccessAttributeType))
                {
                    var access = ((AccessAttribute)item).Access;
                    switch (access)
                    {
                        case AccessModifier.Protected:
                            attr |= TypeAttributes.NestedFamily;
                            break;
                        case AccessModifier.Assembly:
                            attr |= TypeAttributes.NestedAssembly;
                            break;
                        case AccessModifier.ProtectedAndAssembly:
                            attr |= TypeAttributes.NestedFamANDAssem;
                            break;
                        case AccessModifier.ProtectedOrAssembly:
                            attr |= TypeAttributes.NestedFamORAssem;
                            break;
                        case AccessModifier.Private:
                            attr |= TypeAttributes.NestedPrivate;
                            break;
                        default:
                            attr |= TypeAttributes.Public;
                            break;
                    }
                    accessSet = true;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.ValueTypeAttribute.AttributeType))
                {
                    attr |= TypeAttributes.Sealed | TypeAttributes.AutoLayout;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.AbstractAttribute.AttributeType))
                {
                    attr |= TypeAttributes.Abstract;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.InterfaceAttribute.AttributeType))
                {
                    attr ^= attr & TypeAttributes.Sealed;
                    attr |= TypeAttributes.Interface | TypeAttributes.Abstract;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.VirtualAttribute.AttributeType))
                {
                    attr ^= attr & TypeAttributes.Sealed;
                }
                else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.StaticTypeAttribute.AttributeType))
                {
                    attr |= TypeAttributes.Abstract | TypeAttributes.Sealed;
                }
            }
            if (!accessSet)
            {
                attr |= TypeAttributes.Public;
            }
            return attr;
        }

        #endregion

        #region Namespace Business

        public void AddType(TypeDefinition Type)
        {
            var resolvedType = GetResolvedType();
            Type.Namespace = "";
            resolvedType.NestedTypes.Add(Type);
            Type.DeclaringType = resolvedType;
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return (INamespaceBuilder)DeclareType(new Flame.Build.DescribedType(Name, this));
        }

        public ITypeBuilder DeclareType(IType Template)
        {
            return CecilTypeBuilder.DeclareType(this, Template);
        }

        INamespace IMemberBuilder<INamespace>.Build()
        {
            return this;
        }

        public ModuleDefinition GetModule()
        {
            return GetTypeReference().Module;
        }

        #endregion
    }
}
