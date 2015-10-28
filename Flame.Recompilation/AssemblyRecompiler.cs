using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using Flame.Recompilation.Emit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class AssemblyRecompiler : IAssemblyRecompiler, IBodyPassEnvironment
    {
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager, PassSuite Passes, RecompilationSettings Settings)
        {
            this.TargetAssembly = TargetAssembly;
            this.Log = Log;
            this.TaskManager = TaskManager;
            this.Settings = Settings;
            this.Passes = Passes;
            InitCache();
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager, PassSuite Passes)
            : this(TargetAssembly, Log, TaskManager, Passes, new RecompilationSettings())
        {
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager, RecompilationSettings Settings)
            : this(TargetAssembly, Log, TaskManager, PassSuite.CreateDefault(Log), Settings)
        {
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager)
            : this(TargetAssembly, Log, TaskManager, PassSuite.CreateDefault(Log), new RecompilationSettings())
        {
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log)
            : this(TargetAssembly, Log, new AsyncTaskManager())
        {
        }

        public IAssemblyBuilder TargetAssembly { [Pure] get; private set; }
        public ICompilerLog Log { [Pure] get; private set; }
        public IAsyncTaskManager TaskManager { [Pure] get; private set; }
        public RecompilationSettings Settings { [Pure] get; private set; }
        public PassSuite Passes { [Pure] get; private set; }

        public bool RecompileBodies { [Pure] get { return Settings.RecompileBodies; } }
        public bool LogRecompilation { [Pure] get { return Settings.LogRecompilation; } }
        public IEnvironment Environment { [Pure] get { return cachedEnvironment.Value; } }

        #region Cache

        private void InitCache()
        {
            this.TypeCache = new CompilationCache<IType>(GetNewType, TaskManager);
            this.FieldCache = new CompilationCache<IField>(GetNewField, TaskManager);
            this.PropertyCache = new CompilationCache<IProperty>(GetNewProperty, TaskManager);
            this.MethodCache = new CompilationCache<IMethod>(GetNewMethod, TaskManager);
            this.NamespaceCache = new CompilationCache<INamespace>(GetNewNamespace, TaskManager);
            this.recompiledAssemblies = new Dictionary<IAssembly, RecompilationOptions>();
            this.cachedEnvironment = new Lazy<IEnvironment>(() => TargetAssembly.CreateBinder().Environment);
            this.methodBodies = new AsyncDictionary<IMethod, IStatement>();

            this.GlobalMetdata = new RandomAccessOptions();
            this.typeMetadata = new Dictionary<IType, RandomAccessOptions>();
        }

        public CompilationCache<IType> TypeCache { [Pure] get; private set; }
        public CompilationCache<IField> FieldCache { [Pure] get; private set; }
        public CompilationCache<IProperty> PropertyCache { [Pure] get; private set; }
        public CompilationCache<IMethod> MethodCache { [Pure] get; private set; }
        public CompilationCache<INamespace> NamespaceCache { [Pure] get; private set; }

        public RandomAccessOptions GlobalMetdata { [Pure] get; private set; }
        private Dictionary<IType, RandomAccessOptions> typeMetadata;

        private Dictionary<IAssembly, RecompilationOptions> recompiledAssemblies;
        private Lazy<IEnvironment> cachedEnvironment;
        private AsyncDictionary<IMethod, IStatement> methodBodies;

        public RandomAccessOptions GetTypeMetadata(IType Type)
        {
            return typeMetadata[Type];
        }

        #endregion

        #region IsExternal

        private static bool HasExternalAttribute(IMember Member)
        {
            return Member.Attributes.Any(item => item.AttributeType.FullName == "Flame.RT.ExternalAttribute");
        }

        public bool IsExternal(IAssembly Assembly)
        {
            if (Assembly == null)
            {
                return true;
            }
            lock (TargetAssembly)
            {
                if (Assembly.Equals(TargetAssembly))
                {
                    return true;
                }
            }
            lock (recompiledAssemblies)
            {
                return !recompiledAssemblies.ContainsKey(Assembly);
            }
        }
        public bool IsExternal(INamespace Namespace)
        {
            if (Namespace is IType)
            {
                return IsExternal((IType)Namespace);
            }

            return IsExternal(Namespace.DeclaringAssembly);
        }
        public bool IsExternal(IType Type)
        {
            if (Type.HasAttribute(PrimitiveAttributes.Instance.RecompileAttribute.AttributeType))
            {
                return false;
            }
            else if (Type.get_IsContainerType())
            {
                return IsExternal(Type.AsContainerType().ElementType);
            }
            else if (Type.get_IsGenericParameter())
            {
                var declMember = ((IGenericParameter)Type).DeclaringMember;
                return IsExternal(declMember);
            }
            else if (Type.get_IsGenericInstance())
            {
                return IsExternal(Type.GetGenericDeclaration()) && Type.GetGenericArguments().All(IsExternal);
            }
            else if (Type.get_IsDelegate())
            {
                var method = MethodType.GetMethod(Type);
                return IsExternal(method.ReturnType) && method.Parameters.GetTypes().All(IsExternal);
            }
            else if (HasExternalAttribute(Type))
            {
                return true;
            }
            var declNs = Type.DeclaringNamespace;
            if (declNs == null)
            {
                return true;
            }
            else
            {
                return IsExternal(declNs);
            }
        }
        public bool IsExternal(ITypeMember Member)
        {
            if (HasExternalAttribute(Member))
            {
                return true;
            }

            var declType = Member.DeclaringType;
            if (declType == null)
            {
                return true;
            }
            if (Member is IGenericMember)
            {
                var genMember = (IGenericMember)Member;
                if (genMember.get_IsGenericInstance())
                {
                    var genArgs = genMember.GetGenericArguments();
                    if (!genArgs.All(IsExternal))
                    {
                        return false;
                    }
                }
            }
            return IsExternal(declType);
        }
        public bool IsExternal(IMember Member)
        {
            if (Member is ITypeMember)
            {
                return IsExternal((ITypeMember)Member);
            }
            else if (Member is IType)
            {
                return IsExternal((IType)Member);
            }
            else if (Member is INamespace)
            {
                return IsExternal((INamespace)Member);
            }
            else if (Member is IAssembly)
            {
                return IsExternal((IAssembly)Member);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public bool IsExternalStrict(ITypeMember Member)
        {
            var declType = Member.DeclaringType;

            if (HasExternalAttribute(Member))
            {
                return true;
            }

            return declType == null ? true : IsExternalStrict(declType);
        }

        public bool IsExternalStrict(IType Type)
        {
            if (Type.HasAttribute(PrimitiveAttributes.Instance.RecompileAttribute.AttributeType))
            {
                return false;
            }
            else if (HasExternalAttribute(Type) || Type.get_IsContainerType())
            {
                return true;
            }
            else if (Type.get_IsGenericParameter())
            {
                var declMember = ((IGenericParameter)Type).DeclaringMember;
                return IsExternalStrict(declMember);
            }
            var declNs = Type.DeclaringNamespace;
            if (declNs == null)
            {
                return true;
            }
            else
            {
                return IsExternal(declNs);
            }
        }

        public bool IsExternalStrict(IMember Member)
        {
            if (HasExternalAttribute(Member))
            {
                return true;
            }
            else if (Member is ITypeMember)
            {
                return IsExternalStrict((ITypeMember)Member);
            }
            else if (Member is IType)
            {
                return IsExternalStrict((IType)Member);
            }
            else if (Member is INamespace)
            {
                return IsExternal((INamespace)Member);
            }
            else if (Member is IAssembly)
            {
                return IsExternal((IAssembly)Member);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region Members

        public IMember GetMember(IMember SourceMember)
        {
            if (SourceMember is IAssembly)
            {
                if (IsExternal((IAssembly)SourceMember))
                {
                    return SourceMember;
                }
                else
                {
                    return TargetAssembly;
                }
            }
            else if (SourceMember is IType)
            {
                return GetType((IType)SourceMember);
            }
            else
            {
                return GetTypeMember(SourceMember as ITypeMember);
            }
        }

        public ITypeMember GetTypeMember(ITypeMember SourceMember)
        {
            if (SourceMember is IMethod)
            {
                return GetMethod((IMethod)SourceMember);
            }
            else if (SourceMember is IProperty)
            {
                return GetProperty((IProperty)SourceMember);
            }
            else if (SourceMember is IField)
            {
                return GetField((IField)SourceMember);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public ITypeMember[] GetTypeMembers(ITypeMember[] SourceMembers)
        {
            ITypeMember[] results = new ITypeMember[SourceMembers.Length];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GetTypeMember(SourceMembers[i]);
            }
            return results;
        }

        #endregion

        #region Types

        private MemberCreationResult<IType> GetNewType(IType SourceType)
        {
            if (SourceType.get_IsDelegate())
            {
                return new MemberCreationResult<IType>(MethodType.Create(GetMethod(MethodType.GetMethod(SourceType))));
            }
            else if (SourceType.get_IsIntersectionType())
            {
                var interType = (IntersectionType)SourceType;

                return new IntersectionType(GetType(interType.First), GetType(interType.Second));
            }
            else if (IsExternal(SourceType))
            {
                return new MemberCreationResult<IType>(SourceType);
            }
            else if (SourceType.get_IsContainerType())
            {
                var containerType = SourceType.AsContainerType();
                var recompiledElemType = GetType(containerType.ElementType);
                if (SourceType.get_IsVector())
                {
                    return new MemberCreationResult<IType>(recompiledElemType.MakeVectorType(containerType.AsVectorType().Dimensions));
                }
                else if (SourceType.get_IsPointer())
                {
                    return new MemberCreationResult<IType>(recompiledElemType.MakePointerType(containerType.AsPointerType().PointerKind));
                }
                else if (SourceType.get_IsArray())
                {
                    return new MemberCreationResult<IType>(recompiledElemType.MakeArrayType(containerType.AsArrayType().ArrayRank));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (SourceType.get_IsRecursiveGenericInstance())
            {
                var recompiledGenericDecl = GetType(SourceType.GetRecursiveGenericDeclaration());
                var recompiledGenericArgs = GetTypes(SourceType.GetRecursiveGenericArguments());
                return new MemberCreationResult<IType>(recompiledGenericDecl.MakeRecursiveGenericType(recompiledGenericArgs));
            }
            else if (SourceType.get_IsGenericParameter())
            {
                var declType = ((IGenericParameter)SourceType).DeclaringMember;
                var recompiledDeclType = (IGenericMember)GetMember(declType);
                return new MemberCreationResult<IType>(recompiledDeclType.GenericParameters.Single((item) => item.Name == SourceType.Name));
            }
            else
            {
                return RecompileTypeHeader(SourceType);
            }
        }

        private ITypeBuilder GetTypeBuilder(IType SourceType)
        {
            if (SourceType.get_IsRecursiveGenericInstance())
            {
                SourceType = SourceType.GetRecursiveGenericDeclaration();
            }
            var type = GetType(SourceType);
            if (type is ITypeBuilder)
            {
                return (ITypeBuilder)type;
            }
            else
            {
                throw new InvalidCastException("The requested type was no type builder.");
            }
        }

        public IType GetType(IType SourceType)
        {
            return TypeCache.Get(SourceType);
        }
        public IEnumerable<IType> GetTypes(IEnumerable<IType> SourceTypes)
        {
            return TypeCache.GetMany(SourceTypes);
        }
        public IType[] GetTypes(IType[] SourceTypes)
        {
            return TypeCache.GetMany(SourceTypes);
        }

        #endregion

        #region Reverse type lookup

        public IType GetOriginalType(IType RecompiledType)
        {
            return TypeCache.GetOriginal(RecompiledType);
        }

        #endregion

        #region Operators

        public IMethod GetOperatorOverload(Operator Op, IEnumerable<IType> Types)
        {
            var reverseTypes = Types.Select(GetOriginalType);
            var paramTypes = reverseTypes.Zip(Types, (a, b) => a == null ? b : a);
            var overload = Op.GetOperatorOverload(paramTypes);
            if (overload == null)
            {
                return null;
            }
            return GetMethod(overload);
        }

        public IMethod GetOperatorOverload(Operator Op, params IType[] Types)
        {
            return GetOperatorOverload(Op, (IEnumerable<IType>)Types);
        }

        #endregion

        #region GetGenericResolver

        /// <summary>
        /// Gets a generic type resolver for the given type, which
        /// must either be a generic resolver itself, or a generic type
        /// instance.
        /// </summary>
        /// <param name="Type"></param>
        /// <returns></returns>
        private static IGenericResolver GetGenericResolver(IType Type)
        {
            if (Type is IGenericResolver)
            {
                return (IGenericResolver)Type;
            }
            else if (Type is GenericTypeBase)
            {
                return ((GenericTypeBase)Type).Resolver;
            }
            else
            {
                throw new InvalidOperationException("Could not get a generic type resolver for type '" + Type.FullName + 
                                                    "' because it is neither a generic resolver nor a generic instance type.");
            }
        }

        #endregion

        #region Fields

        public IField GetField(IField SourceField)
        {
            return FieldCache.Get(SourceField);
        }

        private MemberCreationResult<IField> GetNewField(IField SourceField)
        {
            if (IsExternal(SourceField))
            {
                return new MemberCreationResult<IField>(SourceField);
            }
            else
            {
                if (SourceField is GenericInstanceField)
                {
                    var recompGenericField = GetField(SourceField.GetRecursiveGenericDeclaration());
                    var recompDeclType = GetType(SourceField.DeclaringType);
                    return new MemberCreationResult<IField>(new GenericInstanceField(recompGenericField, GetGenericResolver(recompDeclType), recompDeclType));
                }
                else if (IsExternalStrict(SourceField))
                {
                    var recompDeclType = GetType(SourceField.DeclaringType);
                    return new MemberCreationResult<IField>(recompDeclType.GetField(SourceField.Name, SourceField.IsStatic));
                }
                else
                {
                    var tb = GetTypeBuilder(SourceField.DeclaringType);
                    return RecompileField(tb, SourceField);
                }
            }
        }

        #endregion

        #region Properties

        public IProperty GetProperty(IProperty SourceProperty)
        {
            return PropertyCache.Get(SourceProperty);
        }

        public IPropertyBuilder GetPropertyBuilder(IProperty SourceProperty)
        {
            return (IPropertyBuilder)GetProperty(SourceProperty);
        }

        private MemberCreationResult<IProperty> GetNewProperty(IProperty SourceProperty)
        {
            if (IsExternal(SourceProperty))
            {
                return new MemberCreationResult<IProperty>(SourceProperty);
            }
            else
            {
                if (SourceProperty is GenericInstanceProperty)
                {
                    var recompGenericProperty = GetProperty(SourceProperty.GetRecursiveGenericDeclaration());
                    var recompDeclType = GetType(SourceProperty.DeclaringType);
                    return new MemberCreationResult<IProperty>(new GenericInstanceProperty(recompGenericProperty, GetGenericResolver(recompDeclType), recompDeclType));
                }
                else if (IsExternalStrict(SourceProperty))
                {
                    var recompDeclType = GetType(SourceProperty.DeclaringType);
                    IProperty result;
                    if (SourceProperty.get_IsIndexer())
                    {
                        result = recompDeclType.GetIndexer(SourceProperty.IsStatic, GetTypes(SourceProperty.IndexerParameters.GetTypes()));
                    }
                    else
                    {
                        result = recompDeclType.Properties.GetProperty(SourceProperty.Name, SourceProperty.IsStatic, GetType(SourceProperty.PropertyType), GetTypes(SourceProperty.IndexerParameters.GetTypes()).ToArray());
                    }
                    System.Diagnostics.Debug.Assert(result != null);
                    return new MemberCreationResult<IProperty>(result);
                }
                else
                {
                    var tb = GetTypeBuilder(SourceProperty.DeclaringType);
                    return RecompilePropertyHeader(tb, SourceProperty);
                }
            }
        }

        #endregion

        #region Methods

        private MemberCreationResult<IMethod> GetNewAccessor(IAccessor SourceAccessor)
        {
            if (SourceAccessor is GenericInstanceAccessor)
            {
                var recompAccessor = GetAccessor((IAccessor)SourceAccessor.GetRecursiveGenericDeclaration());
                var recompProperty = (GenericInstanceProperty)GetProperty(SourceAccessor.DeclaringProperty);

                return new MemberCreationResult<IMethod>(new GenericInstanceAccessor(recompAccessor, recompProperty.Resolver, recompProperty));
            }
            else if (IsExternalStrict(SourceAccessor.DeclaringType))
            {
                var declProperty = GetProperty(SourceAccessor.DeclaringProperty);
                return new MemberCreationResult<IMethod>(declProperty.GetAccessor(SourceAccessor.AccessorType));
            }
            else
            {
                var recompiledProperty = GetPropertyBuilder(SourceAccessor.DeclaringProperty);
                return RecompileAccessor(recompiledProperty, SourceAccessor);
            }
        }

        private MemberCreationResult<IMethod> GetNewMethod(IMethod SourceMethod)
        {
            if (SourceMethod.get_IsAnonymous())
            {
                var visitor = new RecompilingTypeVisitor(this);
                return new MemberCreationResult<IMethod>(MethodType.GetMethod(visitor.Convert(MethodType.Create(SourceMethod))));
            }
            else if (IsExternal(SourceMethod))
            {
                return new MemberCreationResult<IMethod>(SourceMethod);
            }
            else if (SourceMethod.get_IsGenericInstance())
            {
                var recompiledGeneric = GetMethod(SourceMethod.GetGenericDeclaration());
                var recompiledGenArgs = GetTypes(SourceMethod.GetGenericArguments());
                return new MemberCreationResult<IMethod>(recompiledGeneric.MakeGenericMethod(recompiledGenArgs));
            }
            else if (SourceMethod is IAccessor)
            {
                return GetNewAccessor((IAccessor)SourceMethod);
            }
            else if (SourceMethod is GenericInstanceMethod)
            {
                var recompGenericMethod = GetMethod(SourceMethod.GetRecursiveGenericDeclaration());
                var recompDeclType = GetType(SourceMethod.DeclaringType);
                return new MemberCreationResult<IMethod>(new GenericInstanceMethod(recompGenericMethod, GetGenericResolver(recompDeclType), recompDeclType));
            }
            else if (IsExternalStrict(SourceMethod))
            {
                var recompDeclType = GetType(SourceMethod.DeclaringType);
                return new MemberCreationResult<IMethod>(recompDeclType.GetMethod(SourceMethod.Name, SourceMethod.IsStatic, GetType(SourceMethod.ReturnType), GetTypes(SourceMethod.GetParameters().GetTypes())));
            }
            else
            {
                return RecompileMethod(GetTypeBuilder(SourceMethod.DeclaringType), SourceMethod);
            }
        }

        public IMethod GetMethod(IMethod SourceMethod)
        {
            return MethodCache.Get(SourceMethod);
        }

        public IMethod[] GetMethods(IMethod[] SourceMethods)
        {
            return MethodCache.GetMany(SourceMethods);
        }

        public IAccessor GetAccessor(IAccessor SourceAccessor)
        {
            return (IAccessor)GetMethod(SourceAccessor);
        }

        public IAccessor[] GetAccessors(IAccessor[] SourceAccessors)
        {
            return GetMethods(SourceAccessors).Cast<IAccessor>().ToArray();
        }

        #endregion

        #region Namespaces

        private MemberCreationResult<INamespace> GetNewNamespace(INamespace SourceNamespace)
        {
            if (IsExternal(SourceNamespace))
            {
                return new MemberCreationResult<INamespace>(SourceNamespace);
            }
            else
            {
                return DeclareNamespaceBuilder(SourceNamespace.FullName);
            }
        }

        private MemberCreationResult<INamespace> DeclareNamespaceBuilder(string FullName)
        {
            // Look for a pre-existing namespace first.
            var preNs = NamespaceCache.FirstOrDefault(item => item is INamespaceBuilder && item.FullName == FullName);
            if (preNs != null)
            {
                return new MemberCreationResult<INamespace>(preNs);
            }
            else
            {
                // Couldn't find an existing namespace with the given name,
                // so create a shiny new namespace instead.
                return DeclareNewNamespace(FullName);
            }
        }

        private MemberCreationResult<INamespace> DeclareNewNamespace(string Name)
        {
            int lastDotIndex = Name.LastIndexOf('.');
            if (lastDotIndex < 0)
            {
                return new MemberCreationResult<INamespace>(TargetAssembly.DeclareNamespace(Name), (tgt, src) =>
                {
                    ((INamespaceBuilder)tgt).Initialize();
                });
            }
            else
            {
                string parentName = Name.Substring(0, lastDotIndex);
                string thisName = Name.Substring(lastDotIndex + 1);
                var parent = DeclareNamespaceBuilder(parentName);
                var parentNs = (INamespaceBuilder)parent.Member;

                return new MemberCreationResult<INamespace>(parentNs.DeclareNamespace(thisName), (tgt, src) =>
                {
                    if (parent.Continuation != null)
                    {
                        parent.Continuation(parentNs, null);
                    }
                    ((INamespaceBuilder)tgt).Initialize();
                });
            }
        }

        private INamespaceBuilder GetNamespaceBuilder(INamespace SourceNamespace)
        {
            if (SourceNamespace is IType)
            {
                var recompType = GetTypeBuilder((IType)SourceNamespace);
                if (recompType is INamespaceBuilder)
                {
                    return (INamespaceBuilder)recompType;
                }
            }
            return (INamespaceBuilder)NamespaceCache.Get(SourceNamespace);
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Takes an attribute and produces an equivalent attribute
        /// whose dependencies have been rewritten to target the 
        /// output assembly.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public IAttribute GetAttribute(IAttribute Value)
        {
            if (Value is AssociatedTypeAttribute)
            {
                return new AssociatedTypeAttribute(GetType(((AssociatedTypeAttribute)Value).AssociatedType));
            }
            else if (Value is IConstructedAttribute)
            {
                var ctedAttr = (IConstructedAttribute)Value;
                var ctor = GetMethod(ctedAttr.Constructor);
                var args = ctedAttr.GetArguments();
                return new Flame.Attributes.ConstructedAttribute(ctor, args);
            }
            else
            {
                return Value;
            }
        }

        #endregion

        #region Type Recompilation

        private MemberCreationResult<IType> RecompileTypeHeader(IType SourceType)
        {
            return RecompileTypeHeader(GetNamespaceBuilder(SourceType.DeclaringNamespace), SourceType);
        }

        private MemberCreationResult<IType> RecompileTypeHeader(INamespaceBuilder DeclaringNamespace, IType SourceType)
        {
            IType preexistingType;
            if (TypeCache.TryGet(SourceType, out preexistingType))
            {
                // The type has already been created. This can happen sometimes.
                // For example, if a type's attribute refers a nested type, and the
                // latter's recompilation was started first, then the nested type will
                // be recompiled by virtue of the attribute access *before* the 
                // original recompilation request can be processed.
                // To avoid creating a duplicate type, we can just return the
                // already recompiled type here.
                return new MemberCreationResult<IType>(preexistingType);
            }

            if (LogRecompilation)
            {
                Log.LogEvent(new LogEntry("Status", "Recompiling " + SourceType.FullName));
            }
            var typeTemplate = RecompiledTypeTemplate.GetRecompilerTemplate(this, SourceType);
            var type = DeclaringNamespace.DeclareType(typeTemplate);
            return new MemberCreationResult<IType>(type, (tgt, src) =>
            {
                typeMetadata[src] = new RandomAccessOptions();
                var typeBuilder = (ITypeBuilder)tgt;
                typeBuilder.Initialize();
                RecompileInvariants(typeBuilder, src);
            });
        }

        private void RecompileEntireType(IType Type)
        {
            var recompType = GetType(Type);
            foreach (var item in Type.Fields.Concat<ITypeMember>(Type.Methods))
            {
                GetMember(item);
            }
            foreach (var item in Type.Properties)
            {
                GetProperty(item);
                foreach (var accessor in item.Accessors)
                {
                    GetMethod(accessor);
                }
            }
            if (Type is INamespace)
            {
                foreach (var item in ((INamespace)Type).Types)
                {
                    RecompileEntireType(item);
                }
            }
        }

        #endregion

        #region Field Recompilation

        private MemberCreationResult<IField> RecompileField(ITypeBuilder DeclaringType, IField SourceField)
        {
            var header = RecompileFieldHeader(DeclaringType, SourceField);
            return new MemberCreationResult<IField>(header, (tgt, src) => 
            {
                var fieldBuilder = (IFieldBuilder)tgt;
                fieldBuilder.Initialize();
                RecompileFieldBody(fieldBuilder, src);
            });
        }

        private IFieldBuilder RecompileFieldHeader(ITypeBuilder DeclaringType, IField SourceField)
        {
            var fieldTemplate = RecompiledFieldTemplate.GetRecompilerTemplate(this, SourceField);
            return DeclaringType.DeclareField(fieldTemplate);
        }
        private void RecompileFieldBody(IFieldBuilder TargetField, IField SourceField)
        {
            if (RecompileBodies)
            {
                var initField = SourceField as IInitializedField;
                if (initField != null)
                {
                    var expr = initField.GetValue();
                    if (expr != null)
                    {
                        try
                        {
                            TaskManager.RunSequential(TargetField.SetValue, GetExpression(expr, CreateEmptyMethod(TargetField)));
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(new LogEntry("Unhandled exception while recompiling field", "An unhandled exception was thrown while recompiling value of field '" + SourceField.FullName + "'."));
                            Log.LogException(ex);
                            throw;
                        }
                    }
                }
                TaskManager.RunSequential<IField>(TargetField.Build);
            }
        }

        #endregion

        #region Method Recompilation

        private MemberCreationResult<IMethod> RecompileMethod(ITypeBuilder DeclaringType, IMethod SourceMethod)
        {
            return new MemberCreationResult<IMethod>(RecompileMethodHeader(DeclaringType, SourceMethod), RecompileMethodContinuation);
        }
        private IMethodBuilder RecompileMethodHeader(ITypeBuilder DeclaringType, IMethod SourceMethod)
        {
            return DeclaringType.DeclareMethod(RecompiledMethodTemplate.GetRecompilerTemplate(this, SourceMethod));
        }
        private MemberCreationResult<IMethod> RecompileAccessor(IPropertyBuilder DeclaringProperty, IAccessor SourceAccessor)
        {
            return new MemberCreationResult<IMethod>(RecompileAccessorHeader(DeclaringProperty, SourceAccessor), RecompileMethodContinuation);
        }
        private IMethodBuilder RecompileAccessorHeader(IPropertyBuilder DeclaringProperty, IAccessor SourceAccessor)
        {
            return DeclaringProperty.DeclareAccessor(SourceAccessor.AccessorType, RecompiledMethodTemplate.GetRecompilerTemplate(this, SourceAccessor));
        }
        private void RecompileMethodContinuation(IMethod Target, IMethod Source)
        {
            var methodBuilder = (IMethodBuilder)Target;
            methodBuilder.Initialize();
            RecompileMethodBody(methodBuilder, Source);
        }

        private static IConverter<IType, IType> CreateGenericConverter(IEnumerable<IGenericParameter> Parameters, IEnumerable<IType> Arguments)
        {
            var tMap = new Dictionary<IType, IType>();
            foreach (var item in Parameters.Zip(Arguments, Tuple.Create))
            {
                tMap[item.Item1] = item.Item2;
            }
            return new TypeMappingConverter(tMap);
        }

        private IStatement GetMethodBodyCore(IMethod SourceMethod)
        {
            if (SourceMethod.get_IsAbstract() || SourceMethod.DeclaringType.get_IsInterface())
            {
                return null;
            }

            if (SourceMethod is GenericInstanceMethod)
            {
                var instMethod = (GenericInstanceMethod)SourceMethod;
                var genBody = GetMethodBody(instMethod.Declaration);
                if (genBody == null)
                {
                    return null;
                }

                var genParams = instMethod.DeclaringType.GetRecursiveGenericParameters();
                var genArgs = instMethod.DeclaringType.GetRecursiveGenericArguments();
                var conv = CreateGenericConverter(genParams, genArgs);
                return MemberNodeVisitor.ConvertTypes(conv, genBody);
            }
            else if (SourceMethod is GenericMethod)
            {
                var instMethod = (GenericMethod)SourceMethod;
                var genBody = GetMethodBody(instMethod.Declaration);
                if (genBody == null)
                {
                    return null;
                }

                var genParams = instMethod.Declaration.GenericParameters;
                var genArgs = instMethod.GenericArguments;
                var conv = CreateGenericConverter(genParams, genArgs);
                return MemberNodeVisitor.ConvertTypes(conv, genBody);
            }

            var bodyMethod = SourceMethod as IBodyMethod;
            if (bodyMethod == null)
            {
                return null;
            }

            try
            {
                return Passes.OptimizeBody(this, bodyMethod);
            }
            catch (Exception ex)
            {
                Log.LogError(new LogEntry("Unhandled exception while getting method body", "An unhandled exception was thrown while acquiring the method body of '" + SourceMethod.FullName + "'."));
                Log.LogException(ex);
                return null;
            }
        }

        private void RecompileMethodBodyCore(IMethodBuilder TargetMethod, IMethod SourceMethod)
        {
            var body = GetMethodBody(SourceMethod);

            if (body == null)
            {
                if (!SourceMethod.get_IsAbstract() && !SourceMethod.DeclaringType.get_IsInterface())
                {
                    Log.LogError(new LogEntry("Recompilation error", "Could not find a method body for '" + SourceMethod.FullName + "'."));
                }
                return;
            }

            try
            {
                var bodyStatement = GetStatement(body, TargetMethod);

                var targetBody = TargetMethod.GetBodyGenerator();
                var block = bodyStatement.Emit(targetBody);
                TaskManager.RunSequential(TargetMethod.SetMethodBody, block);

                TaskManager.RunSequential<IMethod>(TargetMethod.Build);
            }
            catch (AbortCompilationException)
            {
                throw; // Just let this one fly by.
            }
            catch (Exception ex)
            {
                Log.LogError(new LogEntry("Unhandled exception while recompiling method", "An unhandled exception was thrown while recompiling method '" + SourceMethod.FullName + "'."));
                Log.LogException(ex);
            }
        }
        private void RecompileMethodBody(IMethodBuilder TargetMethod, IMethod SourceMethod)
        {
            if (RecompileBodies)
            {
                TaskManager.RunAsync(() => RecompileMethodBodyCore(TargetMethod, SourceMethod));
            }
        }

        #endregion

        #region Property Recompilation

        private MemberCreationResult<IProperty> RecompilePropertyHeader(ITypeBuilder DeclaringType, IProperty SourceProperty)
        {
            return new MemberCreationResult<IProperty>(
                DeclaringType.DeclareProperty(RecompiledPropertyTemplate.GetRecompilerTemplate(this, SourceProperty)), 
                (tgt, src) => ((IPropertyBuilder)tgt).Initialize());
        }

        #endregion

        #region Invariant Recompilation

        private void RecompileInvariant(IInvariantTypeBuilder DeclaringType, IInvariant Invariant)
        {
            var invariantGen = DeclaringType.InvariantGenerator;
            var expr = GetExpression(Invariant.Invariant, CreateEmptyMethod(Invariant));
            var block = expr.Emit(invariantGen.CodeGenerator);
            invariantGen.EmitInvariant(block);
        }

        private void RecompileInvariantsCore(IInvariantTypeBuilder TargetType, IInvariantType SourceType)
        {
            foreach (var item in SourceType.GetInvariants())
            {
                RecompileInvariant(TargetType, item);
            }
        }

        private void RecompileInvariants(ITypeBuilder TargetType, IType SourceType)
        {
            if (TargetType is IInvariantTypeBuilder && SourceType is IInvariantType)
            {
                RecompileInvariants((IInvariantTypeBuilder)TargetType, (IInvariantType)SourceType);
            }
        }

        private void RecompileInvariants(IInvariantTypeBuilder TargetType, IInvariantType SourceType)
        {
            if (RecompileBodies && !Log.Options.GetOption<bool>("omit-invariants", false))
            {
                TaskManager.RunAsync(() => RecompileInvariantsCore(TargetType, SourceType));
            }
        }

        #endregion

        #region Body Recompilation

        public static IMethod CreateEmptyMethod(ITypeMember Member)
        {
            return CreateEmptyMethod(Member.DeclaringType, Member.IsStatic);
        }

        public static IMethod CreateEmptyMethod(IType CurrentType, bool IsStatic)
        {
            var descMethod = new DescribedMethod("", CurrentType);
            descMethod.IsStatic = IsStatic;
            descMethod.AddAttribute(PrimitiveAttributes.Instance.HiddenAttribute);
            return descMethod;
        }

        #region Expressions

        public IExpression GetExpression(IExpression SourceExpression, IMethod Method)
        {
            return (IExpression)Settings.RecompilationPass.Apply(new RecompilationPassArguments(this, Method, SourceExpression));
        }

        #endregion

        #region Statements

        public IStatement GetStatement(IStatement SourceStatement, IMethod Method)
        {
            return (IStatement)Settings.RecompilationPass.Apply(new RecompilationPassArguments(this, Method, SourceStatement));
        }

        #endregion

        #endregion

        #region Assembly Recompilation

        /// <summary>
        /// Adds the given assembly to the list of assemblies to recompile,
        /// with the given recompilation options.
        /// </summary>
        /// <param name="Source"></param>
        public void AddAssembly(IAssembly Source, RecompilationOptions Options)
        {
            lock (recompiledAssemblies)
            {
                recompiledAssemblies[Source] = Options;
            }
        }

        private void RecompileAssemblyCore(IAssembly Source, RecompilationOptions Options)
        {
            foreach (var item in recompiledAssemblies[Source].RecompilationStrategy.GetRoots(Source))
            {
                GetMember(item);
            }
            if (Options.IsMainModule)
            {
                var entryPoint = Source.GetEntryPoint();
                if (entryPoint != null)
                {
                    TargetAssembly.SetEntryPoint(GetMethod(entryPoint));
                }
            }
        }

        public async Task RecompileAsync()
        {
            foreach (var item in recompiledAssemblies)
            {
                RecompileAssemblyCore(item.Key, item.Value);
            }
            
            await TaskManager.WhenDoneAsync();
            TaskManager.RunSequential(() =>
            {
                foreach (var item in PropertyCache.GetAll())
                {
                    if (item is IPropertyBuilder)
                    {
                        ((IPropertyBuilder)item).Build();
                    }
                }
                foreach (var item in TypeCache.GetAll())
                {
                    typeMetadata.Remove(item);
                    if (item is ITypeBuilder)
                    {
                        ((ITypeBuilder)item).Build();
                    }
                }
                foreach (var item in NamespaceCache.GetAll())
                {
                    if (item is INamespaceBuilder)
                    {
                        ((INamespaceBuilder)item).Build();
                    }
                }
            });
        }

        #endregion

        #region IBodyPassEnvironment Implementation

        /// <summary>
        /// Tries to retrieve the method body of the given method. If this cannot be
        /// done, null is returned.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public IStatement GetMethodBody(IMethod Method)
        {
            return GetMethodBodyAsync(Method).Result;
        }

        /// <summary>
        /// Tries to retrieve the method body of the given method. If this cannot be
        /// done, a task containing a null value is returned. This operation may be performed asynchronously.
        /// </summary>
        /// <param name="Method"></param>
        /// <returns></returns>
        public Task<IStatement> GetMethodBodyAsync(IMethod Method)
        {
            return methodBodies.GetOrAdd(Method, null, method => TaskManager.RunAsync(() => GetMethodBodyCore(method)));
        }

        #endregion
    }
}
