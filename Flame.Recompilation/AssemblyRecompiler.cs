using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Recompilation.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class AssemblyRecompiler : IAssemblyRecompiler
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
            : this(TargetAssembly, Log, TaskManager, Passes, new RecompilationSettings(true, false))
        {
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager, RecompilationSettings Settings)
            : this(TargetAssembly, Log, TaskManager, PassSuite.Default, Settings)
        {
        }
        public AssemblyRecompiler(IAssemblyBuilder TargetAssembly, ICompilerLog Log, IAsyncTaskManager TaskManager)
            : this(TargetAssembly, Log, TaskManager, PassSuite.Default, new RecompilationSettings(true, false))
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

        public IEnvironment Environment { [Pure] get; private set; }

        #region Cache

        private void InitCache()
        {
            this.TypeCache = new CompilationCache<IType>(GetNewType, TaskManager);
            this.FieldCache = new CompilationCache<IField>(GetNewField, TaskManager);
            this.PropertyCache = new CompilationCache<IProperty>(GetNewProperty, TaskManager);
            this.MethodCache = new CompilationCache<IMethod>(GetNewMethod, TaskManager);
            this.NamespaceCache = new CompilationCache<INamespace>(GetNewNamespace, TaskManager);
            this.recompiledAssemblies = new List<IAssembly>();
            this.cachedEnvironment = new Lazy<IEnvironment>(() => TargetAssembly.CreateBinder().Environment);
        }

        public CompilationCache<IType> TypeCache { [Pure] get; private set; }
        public CompilationCache<IField> FieldCache { [Pure] get; private set; }
        public CompilationCache<IProperty> PropertyCache { [Pure] get; private set; }
        public CompilationCache<IMethod> MethodCache { [Pure] get; private set; }
        public CompilationCache<INamespace> NamespaceCache { [Pure] get; private set; }
        private List<IAssembly> recompiledAssemblies;
        private Lazy<IEnvironment> cachedEnvironment;

        #endregion

        #region IsExternal

        private static bool HasExternalAttribute(IMember Member)
        {
            return Member.GetAttributes().Any(item => item.AttributeType.FullName == "Flame.RT.ExternalAttribute");
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
                return !recompiledAssemblies.Contains(Assembly);
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
            else if (Type.IsContainerType)
            {
                return IsExternal(Type.AsContainerType().GetElementType());
            }
            else if (Type.get_IsGenericParameter())
            {
                var declMember = ((IGenericParameter)Type).DeclaringMember;
                return IsExternal(declMember);
            }
            if (Type.get_IsGenericInstance())
            {
                return IsExternal(Type.GetGenericDeclaration()) && Type.GetGenericArguments().All(IsExternal);
            }
            if (HasExternalAttribute(Type))
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
            else if (HasExternalAttribute(Type) || Type.IsContainerType)
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

        private IType GetNewType(IType SourceType)
        {
            if (SourceType.get_IsDelegate() && !SourceType.get_IsGenericInstance())
            {
                return MethodType.Create(GetMethod(MethodType.GetMethod(SourceType)));
            }
            else if (SourceType.get_IsIntersectionType())
            {
                var interType = (IntersectionType)SourceType;

                return new IntersectionType(GetType(interType.First), GetType(interType.Second));
            }
            else if (IsExternal(SourceType))
            {
                return SourceType;
            }
            else if (SourceType.IsContainerType)
            {
                var containerType = SourceType.AsContainerType();
                var recompiledElemType = GetType(containerType.GetElementType());
                if (SourceType.get_IsVector())
                {
                    return recompiledElemType.MakeVectorType(containerType.AsVectorType().GetDimensions());
                }
                else if (SourceType.get_IsPointer())
                {
                    return recompiledElemType.MakePointerType(containerType.AsPointerType().PointerKind);
                }
                else if (SourceType.get_IsArray())
                {
                    return recompiledElemType.MakeArrayType(containerType.AsArrayType().ArrayRank);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (SourceType.get_IsRecursiveGenericInstance())
            {
                var genericDecl = SourceType.GetRecursiveGenericDeclaration();
                var recompiledGenericDecl = GetType(genericDecl);
                var genericArgs = SourceType.GetRecursiveGenericArguments();
                var recompiledGenericArgs = GetTypes(genericArgs);
                return recompiledGenericDecl.MakeRecursiveGenericType(recompiledGenericArgs);
            }
            else if (SourceType.get_IsGenericParameter())
            {
                var declType = ((IGenericParameter)SourceType).DeclaringMember;
                var recompiledDeclType = (IGenericMember)GetMember(declType);
                return recompiledDeclType.GetGenericParameters().Single((item) => item.Name == SourceType.Name);
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
        public IType[] GetTypes(IEnumerable<IType> SourceTypes)
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

        #region Fields

        public IField GetField(IField SourceField)
        {
            return FieldCache.Get(SourceField);
        }

        private IField GetNewField(IField SourceField)
        {
            if (IsExternal(SourceField))
            {
                return SourceField;
            }
            else
            {
                if (SourceField.DeclaringType.get_IsRecursiveGenericInstance())
                {
                    var recompGenericField = GetField(SourceField.DeclaringType.GetRecursiveGenericDeclaration().GetField(SourceField.Name, SourceField.IsStatic));
                    var recompDeclType = recompGenericField.DeclaringType.MakeRecursiveGenericType(GetTypes(SourceField.DeclaringType.GetRecursiveGenericArguments()));
                    return recompDeclType.GetField(recompGenericField.Name, recompGenericField.IsStatic);
                }
                else if (IsExternalStrict(SourceField))
                {
                    var recompDeclType = GetType(SourceField.DeclaringType);
                    return recompDeclType.GetField(SourceField.Name, SourceField.IsStatic);
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

        #region GetGenericTypeProperty

        private static IProperty GetGenericTypeProperty(IProperty SourceProperty)
        {
            var genericDeclType = SourceProperty.DeclaringType.GetRecursiveGenericDeclaration();
            var indexParams = SourceProperty.GetIndexerParameters();
            var converter = CreateGenericParameterConverter(genericDeclType, SourceProperty.DeclaringType);
            if (SourceProperty.get_IsIndexer())
            {
                return genericDeclType.GetProperties().Single((item) => item.get_IsIndexer() && CompareGenericMethodParameters(item.GetIndexerParameters(), indexParams, converter));
            }
            else
            {
                return genericDeclType.GetProperties().Single((item) => item.Name == SourceProperty.Name && item.IsStatic == SourceProperty.IsStatic && CompareGenericMethodParameters(item.GetIndexerParameters(), indexParams, converter));
            }
        }

        #endregion

        #region GetSpecificTypeProperty

        private static IProperty GetSpecificTypeProperty(IType SpecificType, IProperty GenericProperty)
        {
            var indexParams = GenericProperty.GetIndexerParameters();
            var properties = SpecificType.GetProperties();
            var converter = CreateGenericParameterConverter(GenericProperty.DeclaringType, SpecificType);
            if (GenericProperty.get_IsIndexer())
            {
                return properties.Single((item) => item.get_IsIndexer() && CompareGenericMethodParameters(indexParams, item.GetIndexerParameters(), converter));
            }
            else
            {
                return properties.Single((item) => item.Name == GenericProperty.Name && item.IsStatic == GenericProperty.IsStatic && CompareGenericMethodParameters(indexParams, item.GetIndexerParameters(), converter));
            }
        }

        #endregion

        public IProperty GetProperty(IProperty SourceProperty)
        {
            return PropertyCache.Get(SourceProperty);
        }

        public IPropertyBuilder GetPropertyBuilder(IProperty SourceProperty)
        {
            return (IPropertyBuilder)GetProperty(SourceProperty);
        }

        private IProperty GetNewProperty(IProperty SourceProperty)
        {
            if (IsExternal(SourceProperty))
            {
                return SourceProperty;
            }
            else
            {
                if (SourceProperty.DeclaringType.get_IsRecursiveGenericInstance())
                {
                    var recompGenericProperty = GetProperty(GetGenericTypeProperty(SourceProperty));
                    var recompDeclType = recompGenericProperty.DeclaringType.MakeRecursiveGenericType(GetTypes(SourceProperty.DeclaringType.GetRecursiveGenericArguments()));
                    return GetSpecificTypeProperty(recompDeclType, recompGenericProperty);
                }
                else if (IsExternalStrict(SourceProperty))
                {
                    var recompDeclType = GetType(SourceProperty.DeclaringType);
                    IProperty result;
                    if (SourceProperty.get_IsIndexer())
                    {
                        result = recompDeclType.GetIndexer(SourceProperty.IsStatic, GetTypes(SourceProperty.GetIndexerParameters().GetTypes()));
                    }
                    else
                    {
                        result = recompDeclType.GetProperties().GetProperty(SourceProperty.Name, SourceProperty.IsStatic, GetType(SourceProperty.PropertyType), GetTypes(SourceProperty.GetIndexerParameters().GetTypes()));
                    }
                    System.Diagnostics.Debug.Assert(result != null);
                    return result;
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

        #region CreateGenericParameterConverter

        private static IConverter<IType, IType> CreateGenericParameterConverter(IType GenericType, IType SpecificType)
        {
            /*var genMap = GenericType.GetGenericParameters()
                .Zip(SpecificType.GetGenericArguments(), (a, b) => new KeyValuePair<IType, IType>(a, b))
                .ToDictionary(item => item.Key, item => item.Value);

            return new TypeMappingConverter(genMap);*/
            return new GenericSubstitutionConverter(GenericType, SpecificType);
        }

        #endregion

        #region CompareGenericTypeMethods

        private static bool CompareGenericTypeMethods(IMethod GenericMethod, IMethod SpecificMethod)
        {
            if (SpecificMethod.IsConstructor == GenericMethod.IsConstructor && (SpecificMethod.IsConstructor || SpecificMethod.Name == GenericMethod.Name) && SpecificMethod.IsStatic == GenericMethod.IsStatic)
            {
                var genericParams = GenericMethod.GetParameters();
                var specificParams = SpecificMethod.GetParameters();

                var typeConv = CreateGenericParameterConverter(GenericMethod.DeclaringType, SpecificMethod.DeclaringType);

                return CompareGenericMethodParameters(genericParams, specificParams, typeConv);
            }
            else
            {
                return false;
            }
        }

        private static bool CompareGenericMethodParameters(IParameter[] genericParams, IParameter[] specificParams, IConverter<IType, IType> TypeConverter)
        {
            if (genericParams.Length != specificParams.Length)
            {
                return false;
            }

            for (int i = 0; i < genericParams.Length; i++)
            {
                var converted = TypeConverter.Convert(genericParams[i].ParameterType);
                if (!converted.Equals(specificParams[i].ParameterType))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region GetGenericTypeMethod

        private IMethod GetGenericTypeMethod(IMethod SourceMethod)
        {
            var genericDeclType = SourceMethod.DeclaringType.GetRecursiveGenericDeclaration();
            IMethod[] methods;
            if (SourceMethod.IsConstructor)
            {
                methods = genericDeclType.GetConstructors();
            }
            else
            {
                methods = genericDeclType.GetMethods();
            }
            return methods.Single((item) => CompareGenericTypeMethods(item, SourceMethod));
        }

        #endregion

        private IMethod GetNewAccessor(IAccessor SourceAccessor)
        {
            IMethod result;
            if (SourceAccessor.DeclaringType.get_IsRecursiveGenericInstance())
            {
                var genericProperty = GetGenericTypeProperty(SourceAccessor.DeclaringProperty);
                var genericAccessor = genericProperty.GetAccessor(SourceAccessor.AccessorType);
                var recompDeclProperty = GetProperty(SourceAccessor.DeclaringProperty);
                var recompGenericAccessor = GetMethod(genericAccessor);
                result = recompDeclProperty.GetAccessor(SourceAccessor.AccessorType);
                System.Diagnostics.Debug.Assert(result != null);
            }
            else if (IsExternalStrict(SourceAccessor.DeclaringType))
            {
                var declProperty = GetProperty(SourceAccessor.DeclaringProperty);
                result = declProperty.GetAccessor(SourceAccessor.AccessorType);
                System.Diagnostics.Debug.Assert(result != null);
            }
            else
            {
                var recompiledProperty = GetPropertyBuilder(SourceAccessor.DeclaringProperty);
                result = RecompileAccessor(recompiledProperty, SourceAccessor);
                System.Diagnostics.Debug.Assert(result != null);
            }
            return result;
        }

        private IMethod GetNewMethod(IMethod SourceMethod)
        {
            if (SourceMethod.get_IsAnonymous())
            {
                return new RecompiledMethodTemplate(this, SourceMethod);
            }

            if (IsExternal(SourceMethod))
            {
                return SourceMethod;
            }

            if (SourceMethod.get_IsGenericInstance())
            {
                var recompiledGeneric = GetMethod(SourceMethod.GetGenericDeclaration());
                var recompiledGenArgs = GetTypes(SourceMethod.GetGenericArguments());
                return recompiledGeneric.MakeGenericMethod(recompiledGenArgs);
            }

            if (SourceMethod is IAccessor)
            {
                return GetNewAccessor((IAccessor)SourceMethod);
            }

            if (SourceMethod.DeclaringType.get_IsRecursiveGenericInstance())
            {
                var recompGenericMethod = GetMethod(GetGenericTypeMethod(SourceMethod));
                var recompDeclType = recompGenericMethod.DeclaringType.MakeRecursiveGenericType(GetTypes(SourceMethod.DeclaringType.GetRecursiveGenericArguments()));
                var recompMethods = recompDeclType.GetMethods().Concat(recompDeclType.GetConstructors());
                return recompMethods.Single((item) => CompareGenericTypeMethods(recompGenericMethod, item));
            }

            if (IsExternalStrict(SourceMethod))
            {
                var recompDeclType = GetType(SourceMethod.DeclaringType);
                return recompDeclType.GetMethod(SourceMethod.Name, SourceMethod.IsStatic, GetType(SourceMethod.ReturnType), GetTypes(SourceMethod.GetParameters().GetTypes()));
            }

            return RecompileMethod(GetTypeBuilder(SourceMethod.DeclaringType), SourceMethod);
        }

        public IMethod GetMethod(IMethod SourceMethod)
        {
            return MethodCache.Get(SourceMethod);
        }

        public IMethod[] GetMethods(IMethod[] SourceMethods)
        {
            return MethodCache.GetMany(SourceMethods);
        }

        public IAccessor[] GetAccessors(IAccessor[] SourceAccessors)
        {
            return GetMethods(SourceAccessors).Cast<IAccessor>().ToArray();
        }

        #endregion

        #region Namespaces

        private INamespace GetNewNamespace(INamespace SourceNamespace)
        {
            if (IsExternal(SourceNamespace))
            {
                return SourceNamespace;
            }
            else
            {
                return DeclareNewNamespace(SourceNamespace.FullName);
            }
        }

        private INamespaceBuilder DeclareNewNamespace(string Name)
        {
            string[] splitName = Name.Split('.');
            if (splitName.Length <= 1)
            {
                return TargetAssembly.DeclareNamespace(Name);
            }
            else
            {
                string parentName = string.Join(".", splitName.Take(splitName.Length - 1));
                return GetNamespaceBuilder(parentName).DeclareNamespace(splitName[splitName.Length - 1]);
            }
        }

        private INamespaceBuilder GetNamespaceBuilder(string FullName)
        {
            var ns = NamespaceCache.FirstOrDefault((item) => item.FullName == FullName) as INamespaceBuilder;
            if (ns == null)
            {
                return DeclareNewNamespace(FullName);
            }
            else
            {
                return ns;
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

        public virtual IAttribute GetAttribute(IAttribute Attribute)
        {
            return Attribute;
        }

        #endregion

        #region Type Recompilation

        private ITypeBuilder RecompileTypeHeader(IType SourceType)
        {
            return RecompileTypeHeader(GetNamespaceBuilder(SourceType.DeclaringNamespace), SourceType);
        }

        private ITypeBuilder RecompileTypeHeader(INamespaceBuilder DeclaringNamespace, IType SourceType)
        {
            if (LogRecompilation)
            {
                Log.LogEvent(new LogEntry("Status", "Recompiling " + SourceType.FullName));
            }
            var typeTemplate = RecompiledTypeTemplate.GetRecompilerTemplate(this, SourceType);
            var type = DeclaringNamespace.DeclareType(typeTemplate);
            if (type is IInvariantTypeBuilder && SourceType is IInvariantType)
            {
                RecompileInvariants((IInvariantType)SourceType, (IInvariantTypeBuilder)type);
            }
            return type;
        }

        private void RecompileEntireType(IType Type)
        {
            var recompType = GetType(Type);
            foreach (var item in Type.GetFields().Concat<ITypeMember>(Type.GetMethods()).Concat(Type.GetConstructors()))
            {
                GetMember(item);
            }
            foreach (var item in Type.GetProperties())
            {
                GetProperty(item);
                foreach (var accessor in item.GetAccessors())
                {
                    GetMethod(accessor);
                }
            }
            if (Type is INamespace)
            {
                foreach (var item in ((INamespace)Type).GetTypes())
                {
                    RecompileEntireType(item);
                }
            }
        }

        #endregion

        #region Field Recompilation

        private IFieldBuilder RecompileField(ITypeBuilder DeclaringType, IField SourceField)
        {
            var header = RecompileFieldHeader(DeclaringType, SourceField);
            RecompileFieldBody(header, SourceField);
            return header;
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
                            TargetField.SetValue(GetExpression(expr, CreateEmptyMethod(TargetField)));
                        }
                        catch (Exception ex)
                        {
                            Log.LogError(new LogEntry("Unhandled exception while recompiling field", "An unhandled exception was thrown while recompiling value of field '" + SourceField.FullName + "'."));
                            Log.LogException(ex);
                            throw;
                        }
                    }
                }
                TargetField.Build();
            }
        }

        #endregion

        #region Method Recompilation

        private IMethodBuilder RecompileMethod(ITypeBuilder DeclaringType, IMethod SourceMethod)
        {
            var header = RecompileMethodHeader(DeclaringType, SourceMethod);
            RecompileMethodBody(header, SourceMethod);
            return header;
        }
        private IMethodBuilder RecompileMethodHeader(ITypeBuilder DeclaringType, IMethod SourceMethod)
        {
            return DeclaringType.DeclareMethod(RecompiledMethodTemplate.GetRecompilerTemplate(this, SourceMethod));
        }
        private IMethodBuilder RecompileAccessor(IPropertyBuilder DeclaringProperty, IAccessor SourceAccessor)
        {
            var header = RecompileAccessorHeader(DeclaringProperty, SourceAccessor);
            RecompileMethodBody(header, SourceAccessor);
            return header;
        }
        private IMethodBuilder RecompileAccessorHeader(IPropertyBuilder DeclaringProperty, IAccessor SourceAccessor)
        {
            return DeclaringProperty.DeclareAccessor(RecompiledAccessorTemplate.GetRecompilerTemplate(this, DeclaringProperty, SourceAccessor));
        }
        private void RecompileMethodBodyCore(IMethodBuilder TargetMethod, IMethod SourceMethod)
        {
            if (SourceMethod.get_IsAbstract() || SourceMethod.DeclaringType.get_IsInterface())
            {
                return;
            }

            var bodyMethod = SourceMethod as IBodyMethod;
            if (bodyMethod == null)
            {
                throw new NotSupportedException("Method '" + SourceMethod.FullName + "' is not a body method, and could not be recompiled.");
            }
            try
            {
                Passes.RecompileBody(this, Environment, (ITypeBuilder)TargetMethod.DeclaringType, TargetMethod, bodyMethod);
                TargetMethod.Build();
            }
            catch (Exception ex)
            {
                Log.LogError(new LogEntry("Unhandled exception while recompiling method", "An unhandled exception was thrown while recompiling method '" + SourceMethod.FullName + "'."));
                Log.LogException(ex);
                throw;
            }
        }
        private void RecompileMethodBody(IMethodBuilder TargetMethod, IMethod SourceMethod)
        {
            if (RecompileBodies)
            {
                TaskManager.QueueAction(() => RecompileMethodBodyCore(TargetMethod, SourceMethod));
            }
        }

        #endregion

        #region Property Recompilation

        private IPropertyBuilder RecompilePropertyHeader(ITypeBuilder DeclaringType, IProperty SourceProperty)
        {
            return DeclaringType.DeclareProperty(RecompiledPropertyTemplate.GetRecompilerTemplate(this, SourceProperty));
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

        private void RecompileInvariantsCore(IInvariantType SourceType, IInvariantTypeBuilder TargetType)
        {
            foreach (var item in SourceType.GetInvariants())
            {
                RecompileInvariant(TargetType, item);
            }
        }

        private void RecompileInvariants(IInvariantType SourceType, IInvariantTypeBuilder TargetType)
        {
            if (RecompileBodies && !Log.Options.GetOption<bool>("omit-invariants", false))
            {
                TaskManager.QueueAction(() => RecompileInvariantsCore(SourceType, TargetType));
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
            var codeGen = new RecompiledCodeGenerator(this, Method);
            var block = SourceExpression.Emit(codeGen);
            return RecompiledCodeGenerator.GetExpression(block);
        }

        #endregion

        #region Statements

        public IStatement GetStatement(IStatement SourceStatement, IMethod Method)
        {
            var codeGen = new RecompiledCodeGenerator(this, Method);
            var block = SourceStatement.Emit(codeGen);
            return RecompiledCodeGenerator.GetStatement(block);
        }

        #endregion

        #endregion

        #region Assembly Recompilation

        private void RecompileAssemblyCore(IAssembly Source, RecompilationOptions Options)
        {
            lock (recompiledAssemblies)
            {
                recompiledAssemblies.Add(Source);
            }
            if (Options.RecompileAll)
            {
                foreach (var item in Source.CreateBinder().GetTypes())
                {
                    RecompileEntireType(item);
                }
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

        public async Task RecompileAsync(IAssembly Source, RecompilationOptions Options)
        {
            RecompileAssemblyCore(Source, Options);

            await TaskManager.WhenDoneAsync();
            foreach (var item in PropertyCache.GetAll())
            {
                if (item is IPropertyBuilder)
                {
                    ((IPropertyBuilder)item).Build();
                }
            }
            foreach (var item in TypeCache.GetAll())
            {
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
        }

        #endregion
    }
}
