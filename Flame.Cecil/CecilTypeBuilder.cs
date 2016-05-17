using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        public CecilTypeBuilder(TypeDefinition Definition, ITypeSignatureTemplate Template, ICecilNamespace DeclaringNamespace, CecilModule Module)
            : base(Definition, Module)
        {
            this.declNs = DeclaringNamespace;
            this.Template = new TypeSignatureInstance(Template, this);
        }

        public TypeSignatureInstance Template { get; private set; }

        #region Initialize

        public void Initialize()
        {
            var typeDef = GetResolvedType();

            var typeAttrs = ExtractTypeAttributes(Template.Attributes.Value);
            if (IsNested && (typeAttrs & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
            {
                typeAttrs &= ~TypeAttributes.Public;
                typeAttrs |= TypeAttributes.NestedPublic;
            }

            typeDef.Name = CreateCLRName(Template);
            typeDef.Attributes = typeAttrs;

            // Evaluate generics first
            var genericTemplates = Template.GenericParameters.Value.ToArray();

            var genParams = CecilGenericParameter.DeclareGenericParameters(typeDef, genericTemplates, Module, this);

            // We should be able to evaluate the base types now.
            var baseTypes = GetGenericTypes(Template.BaseTypes.Value.ToArray(), genParams);

            if (!IsInterface)
            {
                if (IsEnum)
                {
                    typeDef.BaseType = Module.Module.Import(typeof(Enum));
                }
                else if (IsValueType)
                {
                    typeDef.BaseType = Module.Module.Import(typeof(ValueType));
                }
                else
                {
                    var parentType = baseTypes.SingleOrDefault((item) => !item.GetIsInterface());
                    if (parentType != null)
                    {
                        typeDef.BaseType = parentType.GetImportedReference(Module, typeDef);
                    }
                    else
                    {
                        typeDef.BaseType = Module.Module.TypeSystem.Object;
                    }
                }
            }

            foreach (var item in baseTypes.Where(item => item.GetIsInterface()))
            {
                typeDef.Interfaces.Add(item.GetImportedReference(Module, typeDef));
            }

            CecilAttribute.DeclareAttributes(typeDef, this, Template.Attributes.Value);

            if (IsEnum)
            {
                var field = new FieldDefinition("value__",
                    FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName,
                    baseTypes.SingleOrDefault(item => !item.GetIsInterface()).GetImportedReference(Module, typeDef));
                AddField(field);
            }
        }

        #endregion

        #region Declaring Namespace

        private ICecilNamespace declNs;
        public override INamespace DeclaringNamespace
        {
            get
            {
                return declNs;
            }
        }

        #endregion

        #region Properties

        public bool IsNested
        {
            get
            {
                return declNs is ICecilType;
            }
        }

        public bool IsEnum
        {
            get
            {
                return Template.Attributes.Value.Contains(
                    PrimitiveAttributes.Instance.EnumAttribute.AttributeType);
            }
        }

        public override bool IsInterface
        {
            get
            {
                return Template.Attributes.Value.Contains(
                    PrimitiveAttributes.Instance.InterfaceAttribute.AttributeType);
            }
        }

        public override bool IsValueType
        {
            get
            {
                return Template.Attributes.Value.Contains(
                    PrimitiveAttributes.Instance.ValueTypeAttribute.AttributeType);
            }
        }

        #endregion

        #region Static

        private static string CreateCLRName(TypeSignatureInstance Template)
        {
            int genCount = Template.GenericParameters.Value.Count();
            if (genCount == 0)
            {
                return Template.Name.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append(GenericNameExtensions.TrimGenerics(Template.Name.ToString()));
                sb.Append('`');
                sb.Append(genCount);
                return sb.ToString();
            }
        }

        public static CecilTypeBuilder DeclareType(ICecilNamespace CecilNamespace, ITypeSignatureTemplate Template)
        {
            var reference = new TypeDefinition(CecilNamespace.FullName.ToString(), Template.Name.ToString(), (TypeAttributes)0);

            var cecilType = new CecilTypeBuilder(reference, Template, CecilNamespace, CecilNamespace.Module);
            if (CecilNamespace is ICecilType)
            {
                var declType = (ICecilType)CecilNamespace;
                var declGenerics = declType.GenericParameters.ToArray();
                CecilGenericParameter.DeclareGenericParameters(reference, declGenerics, cecilType.Module, cecilType);
            }

            CecilNamespace.AddType(reference);

            return cecilType;
        }

        public static IType[] GetGenericTypes(IType[] SourceTypes, IGenericParameter[] GenericParameters)
        {
            return new GenericParameterTransformer(GenericParameters).Convert(SourceTypes).ToArray();
        }

        public static IType GetGenericType(IType SourceType, IGenericParameter[] GenericParameters)
        {
            return new GenericParameterTransformer(GenericParameters).Convert(SourceType);
        }

        #endregion

        #region ICecilTypeBuilder Implementation

        public virtual IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var method = CecilMethodBuilder.DeclareMethod(this, Template);
            ClearMethodCache();
            return method;
        }

        public virtual IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var field = IsEnum ? CecilField.DeclareEnumField(this, Template) : CecilField.DeclareField(this, Template);
            ClearFieldCache();
            return field;
        }

        public virtual IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            var property = CecilPropertyBuilder.DeclareProperty(this, Template);
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

        public void DeclareBaseType(IType Type)
        {
            var resolvedType = GetResolvedType();
            var importedRef = CecilTypeImporter.Import(Module, Type);
            if (Type.GetIsInterface())
            {
                resolvedType.Interfaces.Add(importedRef);
            }
            else
            {
                resolvedType.BaseType = importedRef;
            }
            ClearBaseTypeCache();
        }

        public IType Build()
        {
            initialValues = null;
            return this;
        }

        private Dictionary<IField, IExpression> initialValues;
        public void SetInitialValue(IField Field, IExpression Value)
        {
            if (initialValues == null)
            {
                initialValues = new Dictionary<IField, IExpression>();
            }
            initialValues[Field] = Value;
        }

        public IEnumerable<IStatement> CreateFieldInitStatements()
        {
            var statements = new List<IStatement>();
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
                    if (item.Value.GetIsConstant())
                    {
                        if (fieldType.GetIsPrimitive())
                        {
                            if (!IsDefaultValue(item.Value, fieldType))
                            {
                                setField = true;
                            }
                        }
                        else
                        {
                            setField = fieldType.GetIsValueType() || !IsNullValue(item.Value);
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
            return (INamespaceBuilder)DeclareType(new TypePrototypeTemplate(new Flame.Build.DescribedType(new SimpleName(Name), this)));
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
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
