using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Emit;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppType : IInvariantTypeBuilder, ICppTemplateMember, IGenericResolverType,
                           INamespaceBranch, INamespaceBuilder, IEquatable<IType>, IAssociatedMember
    {
        public CppType(INamespace DeclaringNamespace, ITypeSignatureTemplate Template, ICppEnvironment Environment)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Template = new TypeSignatureInstance(Template, this);
            this.Templates = new CppTemplateDefinition(this, this.Template.GenericParameters);
            this.Environment = new TemplatedMemberCppEnvironment(Environment, this);
        }

        public INamespace DeclaringNamespace { get; private set; }
        public TypeSignatureInstance Template { get; private set; }
        public CppTemplateDefinition Templates { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public TypeInvariants Invariants { get; private set; }

        public IInvariantGenerator InvariantGenerator
        {
            get { return new Flame.Cpp.Emit.InvariantGenerator(Invariants); }
        }

        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        #region Member Cache

        private void CreateMemberCache()
        {
            this.fields = new List<CppField>();
            this.methods = new List<ICppMethod>();
            this.properties = new List<CppProperty>();
            this.types = new List<CppType>();
            this.globalFriends = new List<ICppMember>(); 
        }

        private List<CppField> fields;
        private List<ICppMethod> methods;
        private List<CppProperty> properties;
        private List<CppType> types;
        private List<ICppMember> globalFriends;

        #endregion

        #region Type properties

        private QualifiedName cachedFullName;
        public QualifiedName FullName
        {
            get
            {
                if (cachedFullName == null)
                {
                    cachedFullName = Name.Qualify(DeclaringNamespace.FullName);
                }
                return cachedFullName;
            }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public UnqualifiedName Name
        {
            get
            {
                return Template.Name;
            }
        }

        public IEnumerable<IType> BaseTypes { get { return GetBaseTypes(); } }
        public IType[] GetBaseTypes()
        {
            return Template.BaseTypes.Value.Select(this.ConvertValueType).ToArray();
        }

        public IBoundObject GetDefaultValue()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region C++ type properties

        /// <summary>
        /// Gets a boolean value that indicates whether the given type should be displayed as a struct.
        /// </summary>
        public bool IsStruct
        {
            get
            {
                return GetMembers().All((item) => item.GetAccess() == AccessModifier.Public);
            }
        }

        /// <summary>
        /// Gets a boolean flag that tells if this type is a static singleton.
        /// </summary>
        public bool IsStaticSingleton
        {
            get
            {
                if (DeclaringNamespace is IType)
                {
                    return this.GetIsSingleton() && this.Name.ToString() == "Static_Singleton";
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsEmptyStaticSingleton
        {
            get
            {
                if (IsStaticSingleton)
                {
                    return fields.Count == 1 && methods.Count == 1 && properties.Count == 1;
                }
                return false;
            }
        }

        public bool EmitType
        {
            get
            {
                return !IsEmptyStaticSingleton;
            }
        }

        #endregion

        #region Members

        private bool ShouldCompileToParentFriend(IMethodSignatureTemplate Method)
        {
            return IsStaticSingleton && new CppMethod(this, Method, Environment).GetIsOperator();
        }

        private IMethodBuilder CompileToParentFriend(IMethodSignatureTemplate Method)
        {
            var declParent = (CppType)((IType)DeclaringNamespace).GetGenericDeclaration();
            return declParent.DeclareFriendMethod(Method);
        }

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var field = new CppField(this, Template, Environment);
            fields.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            if (ShouldCompileToParentFriend(Template))
            {
                return CompileToParentFriend(Template);
            }
            else
            {
                var method = new CppMethod(this, Template, Environment);
                methods.Add(method);
                if (method.IsHashOperator)
                {
                    globalFriends.Add(new CppHashImplementation(this, method));
                }                
                else if (method.GetIsOperator() && BinaryOperation.IsAssignableBinaryOperator(method.GetOperator()) && method.ReturnType.Equals(this))
                {
                    methods.Add(new CppBinaryAssignmentOverload(this, method));
                }
                return method;
            }
        }

        public IMethodBuilder DeclareFriendMethod(IMethodSignatureTemplate Template)
        {
            var method = new CppMethod(this, Template, Environment, true, true);
            globalFriends.Add(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            var property = new CppProperty(this, Template, Environment);
            properties.Add(property);
            return property;
        }

        public IEnumerable<IField> Fields
        {
            get
            {
                if (Invariants.HasInvariants && !Invariants.InheritsInvariants)
                {
                    return fields.With(Invariants.IsCheckingInvariantsField);
                }
                else
                {
                    return fields;
                }
            }
        }

        public IEnumerable<IMethod> Methods
        {
            get
            {
                var results = methods.Concat(globalFriends.OfType<IMethod>()).With<IMethod>(new ImplicitCopyConstructor(this));
                if (Invariants.HasInvariants)
                {
                    if (!Invariants.InheritsInvariants)
                    {
                        results = results.With(Invariants.CheckInvariantsMethod);
                    }
                    if (!Invariants.CheckInvariantsImplementationMethod.InlineTestBlock)
                    {
                        results = results.With(Invariants.CheckInvariantsImplementationMethod.ToCppMethod());
                    }
                }
                return results;
            }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return properties; }
        }

        public ITypeMember[] GetMembers()
        {
            return GetCppMembers().Concat(globalFriends).OfType<ITypeMember>().ToArray();
        }

        public IEnumerable<ICppMember> GetCppMembers()
        {
            var results = fields.Concat<ICppMember>(methods).Concat(properties).Concat(types);
            if (Invariants.HasInvariants)
            {
                if (!Invariants.InheritsInvariants)
                {
                    results = results.With(Invariants.CheckInvariantsMethod.ToCppMethod())
                                     .With(Invariants.IsCheckingInvariantsField);
                }
                if (!Invariants.CheckInvariantsImplementationMethod.InlineTestBlock)
                {
                    results = results.With(Invariants.CheckInvariantsImplementationMethod.ToCppMethod());
                }
            }
            return results;
        }

        #endregion

        #region Nested types

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return types; }
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return (INamespaceBuilder)DeclareType(new TypePrototypeTemplate(new DescribedType(new SimpleName(Name), this)));
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var type = new CppType((INamespace)this.MakeGenericType(this.GenericParameters), Template, Environment);
            types.Add(type);
            return type;
        }

        #endregion

        #region Generics

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return this.Templates.GetGenericParameters(); }
        }

        #endregion

        public IType Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Invariants = new TypeInvariants(this, Environment);
            CreateMemberCache();
        }

        INamespace IMemberBuilder<INamespace>.Build()
        {
            return this;
        }

        #region ICppMember Implementation

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return GetMembers().GetDependencies().MergeDependencies(globalFriends.GetDependencies()).MergeDependencies(GetBaseTypes().GetDependencies()); }
        }

        #region MemberToAccessGroup

        private static void MemberToAccessGroup(ICppMember Member, IDictionary<string, IList<ICppMember>> AccessGroups)
        {
            if (Member is IProperty)
            {
                var prop = (IProperty)Member;
                if (!prop.HasUniformAccess())
                {
                    foreach (var item in prop.Accessors)
                    {
                        MemberToAccessGroup((ICppMember)item, AccessGroups);
                    }
                    return;
                }
            }

            string modifier;
            switch (Member.GetAccess())
            {
                case AccessModifier.Private:
                    modifier = "private";
                    break;
                case AccessModifier.Protected:
                case AccessModifier.ProtectedAndAssembly:
                    modifier = "protected";
                    break;
                default:
                    modifier = "public";
                    break;
            }
            if (!AccessGroups.ContainsKey(modifier))
            {
                AccessGroups[modifier] = new List<ICppMember>();
            }
            AccessGroups[modifier].Add(Member);
        }

        #endregion

        private void AppendInheritanceCode(CodeBuilder cb, IType Type)
        {
            cb.Append("public ");
            if (Type.GetIsInterface())
            {
                cb.Append("virtual ");
            }
            cb.Append(TypeNamer.Name(Type, (IType)this));
        }

        private CodeBuilder GetInheritanceCode()
        {
            var bTypes = GetBaseTypes();
            if (bTypes.Length == 0)
            {
                return new CodeBuilder();
            }
            else
            {
                var cb = new CodeBuilder();
                cb.Append(" : ");
                AppendInheritanceCode(cb, bTypes[0]);
                for (int i = 1; i < bTypes.Length; i++)
                {
                    cb.Append(", ");
                    AppendInheritanceCode(cb, bTypes[i]);
                }
                return cb;
            }
        }

        public CodeBuilder GetDeclarationCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.AddCodeBuilder(Templates.GetHeaderCode());
            cb.AddLine(IsStruct ? "struct " : "class ");
            cb.Append(this.GetGenericFreeName());
            cb.Append(GetInheritanceCode());
            return cb;
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();

            if (EmitType)
            {
                cb.AddCodeBuilder(this.GetDocumentationComments());
                cb.AddCodeBuilder(GetDeclarationCode());
                cb.AddLine("{");

                var groups = new Dictionary<string, IList<ICppMember>>();
                foreach (var item in GetCppMembers())
                {
                    MemberToAccessGroup(item, groups);
                }
                bool isStruct = IsStruct;
                foreach (var group in groups.OrderByDescending((item) => item.Key, new AccessStringComparer()))
                {
                    if (!isStruct)
                    {
                        cb.AddLine(group.Key + ":");
                    }
                    cb.IncreaseIndentation();
                    foreach (var item in Environment.TypeDefinitionPacker.Pack(group.Value).Where(item => !item.IsEmpty))
                    {
                        cb.AddCodeBuilder(item.GetHeaderCode());
                        cb.AddEmptyLine();
                    }
                    cb.TrimEnd(); // Gets rid of excessive whitespace
                    cb.DecreaseIndentation();
                }

                cb.AddLine("};");
            }

            return cb;
        }

        public bool HasSourceCode
        {
            get { return GetCppMembers().Any(item => item.HasSourceCode); }
        }

        public CodeBuilder GetSourceCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (EmitType)
            {
                foreach (var item in Environment.TypeDefinitionPacker.Pack(GetCppMembers().Where(item => item.HasSourceCode)))
                {
                    cb.AddCodeBuilder(item.GetSourceCode());
                    cb.AddEmptyLine();
                }
            }

            return cb;
        }

        public override string ToString()
        {
            return GetDeclarationCode().ToString();
        }

        #endregion

        #region Equals/GetHashCode

        public override bool Equals(object obj)
        {
            if (obj is IType)
            {
                return Equals((IType)obj);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(IType other)
        {
            return this.FullName.Equals(other.FullName);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        #endregion

        public IEnumerable<ICppMember> AssociatedMembers
        {
            get { return globalFriends; }
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
