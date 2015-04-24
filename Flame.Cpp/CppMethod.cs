using Flame.Build;
using Flame.CodeDescription;
using Flame.Compiler;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface ICppMethod : IMethod, ICppTemplateMember
    {
    }

    public class CppMethod : IMethodBuilder, ICppMethod, IEquatable<IMethod>
    {
        public CppMethod(IGenericResolverType DeclaringType, IMethod Template, ICppEnvironment Environment, bool IsStatic, bool IsGlobal)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.Templates = new CppTemplateDefinition(this, Template);
            this.Environment = new TemplatedMemberCppEnvironment(Environment, this);
            this.IsStatic = IsStatic;
            this.IsGlobal = IsGlobal;
            this.codeGen = new CppCodeGenerator(this, this.Environment);
            this.built = false;
        }
        public CppMethod(IGenericResolverType DeclaringType, IMethod Template, ICppEnvironment Environment)
            : this(DeclaringType, Template, Environment, Template.IsStatic, false)
        { }

        public IType DeclaringType { get; private set; }
        public IMethod Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }
        public CppTemplateDefinition Templates { get; private set; }
        public bool IsStatic { get; private set; }
        public bool IsGlobal { get; private set; }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return ReturnType.GetDependencies(DeclaringType).MergeDependencies(GetParameters().GetDependencies(DeclaringType)).MergeDependencies(Body.Dependencies); }
        }

        #region Code Generation

        private bool built;
        private CppCodeGenerator codeGen;
        public ICodeGenerator GetBodyGenerator()
        {
            if (built)
            {
                throw new InvalidOperationException();
            }
            return codeGen;
        }

        private ContractBlock methodBody;
        public ICppBlock Body { get { return methodBody as ICppBlock ?? new EmptyBlock(codeGen); } }

        public MethodContract Contract
        {
            get
            {
                return methodBody != null ? methodBody.Contract : new MethodContract(codeGen, Enumerable.Empty<ICppBlock>(), Enumerable.Empty<ICppBlock>());
            }
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.methodBody = Body is ContractBlock ? (ContractBlock)Body : new ContractBlock((ICppBlock)Body, Enumerable.Empty<ICppBlock>(), Enumerable.Empty<ICppBlock>());
        }

        #endregion

        public IMethod Build()
        {
            built = true;
            return this;
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public CodeBuilder GetDocumentationComments()
        {
            var envBuilder = Environment.DocumentationBuilder;
            var provider = new ConcatDocumentationProvider(envBuilder.Provider, new ConstantDocumentationProvider(Contract.DescriptionAttributes));
            var docBuilder = new DocumentationCommentBuilder(provider, envBuilder);
            return docBuilder.GetDocumentationComments(this);
        }

        public virtual string Name
        {
            get
            {
                if (IsConstructor)
                {
                    return DeclaringType.Name;
                }
                else
                {
                    return Template.Name;
                }
            }
        }

        public IMethod[] GetBaseMethods()
        {
            return Template.GetBaseMethods();
        }

        public IMethod GetGenericDeclaration()
        {
            return this;
        }

        public IParameter[] GetParameters()
        {
            var templParams = Template.GetParameters();
            DescribedParameter[] newParams = new DescribedParameter[templParams.Length];
            for (int i = 0; i < templParams.Length; i++)
            {
                newParams[i] = new DescribedParameter(templParams[i].Name, Environment.TypeConverter.Convert(templParams[i].ParameterType));
                foreach (var item in templParams[i].GetAttributes())
                {
                    newParams[i].AddAttribute(item);
                }
            }
            return newParams;
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
        }

        public bool IsExistential
        {
            get { return IsConstructor; }
        }

        public bool IsCast
        {
            get { return this.get_IsCast(); }
        }

        public bool IsOperator
        {
            get { return this.get_IsOperator() || this.IsCast; }
        }

        public bool IsHashOperator
        {
            get { return this.get_IsOperator() && this.GetOperator().Equals(Operator.Hash); }
        }

        public bool IsVirtual
        {
            get { return this.get_IsVirtual() || this.IsPureVirtual; }
        }

        public bool IsPureVirtual
        {
            get { return this.get_IsAbstract() || this.DeclaringType.get_IsInterface(); }
        }

        public bool EmitInline
        {
            get { return (this.get_IsGeneric() && this.DeclaringType.get_IsGenericDeclaration()); }
        }

        /// <summary>
        /// Gets a boolean value that, if true, explains that
        /// a commented version of the method's body will be emitted
        /// in the header file, even though the method is technically 
        /// defined in the source file.
        /// </summary>
        public bool HasPublicBody
        {
            get { return this.Equals(DeclaringType.GetInvariantsCheckImplementationMethod()); }
        }

        public bool IsOverride
        {
            get { return GetBaseMethods().Length > 0; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new DescribedGenericMethodInstance(this, (IGenericResolverType)DeclaringType, TypeArguments);
        }

        public IType ReturnType
        {
            get
            {
                return this.ConvertType(Template.ReturnType);
            }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return this.Templates.GetGenericParameters();
        }

        #region GetCode

        #region GetSharedSignature

        private CodeBuilder GetSharedSignature(bool PrefixName)
        {
            bool isConst = this.get_IsConstant() && !IsExistential;
            bool isCast = IsCast;
            CodeBuilder cb = new CodeBuilder();
            if (!IsExistential && !isCast)
            {
                cb.Append(TypeNamer.Name(ReturnType, this));
                cb.Append(' ');
            }
            if (PrefixName)
            {
                var genDeclType = (IGenericResolverType)DeclaringType.MakeGenericType(DeclaringType.GetGenericParameters());
                cb.Append(TypeNamer.Name(genDeclType, this));
                cb.Append("::");
            }
            if (isCast)
            {
                cb.Append("operator ");
                cb.Append(TypeNamer.Name(ReturnType, this));
            }
            else
            {
                cb.Append(this.GetGenericFreeName());
            }
            cb.Append('(');
            var parameters = GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                var tParam = parameters[i].ParameterType;
                if (isConst && tParam.get_IsPointer() && tParam.AsContainerType().AsPointerType().IsPrimitivePointer())
                {
                    cb.Append("const ");
                }
                cb.Append(TypeNamer.Name(tParam, this));
                cb.Append(' ');
                cb.Append(parameters[i].Name);
            }
            cb.Append(')');
            if (isConst)
            {
                cb.Append(" const");
            }
            return cb;
        }

        #endregion

        public CodeBuilder GetHeaderCode()
        {
            bool isConst = this.get_IsConstant();
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AddCodeBuilder(LocalTemplateDefinition.GetHeaderCode());
            cb.AppendLine();
            if (this.IsStatic && !this.IsGlobal)
            {
                cb.Append("static ");
            }
            else if (this.IsVirtual)
            {
                cb.Append("virtual ");
            }
            cb.Append(GetSharedSignature(false));
            if (IsOverride)
            {
                cb.Append(" override");
            }
            if (this.IsPureVirtual)
            {
                cb.Append(" = 0");
            }
            if (EmitInline)
            {
                cb.AddCodeBuilder(GetBodyCode());
            }
            else
            {
                cb.Append(';');
                if (HasPublicBody) // Emit a sneak peek nonetheless
                {
                    var bodyCode = GetBodyCode();
                    for (int i = 0; i < bodyCode.LineCount; i++)
			        {
                        var line = bodyCode[i];
                        cb.AddLine("// " + line.ToString(new string(' ', 4)));
			        }
                }
            }
            return cb;
        }

        public bool HasSourceCode
        {
            get { return !IsPureVirtual; }
        }

        private CppTemplateDefinition LocalTemplateDefinition
        {
            get
            {
                return IsGlobal ? FullTemplateDefinition : Templates;
            }
        }

        private CppTemplateDefinition FullTemplateDefinition
        {
            get
            {
                var declTemplDefs = DeclaringType.GetFullTemplateDefinition();
                return declTemplDefs.Merge(Templates);
            }
        }

        public CodeBuilder GetBodyCode()
        {
            CodeBuilder cb = new CodeBuilder();
            var body = Body.ImplyEmptyReturns();
            if (this.IsConstructor)
            {
                body = body.ImplyStructInit();
            }
            cb.AddEmbracedBodyCodeBuilder(body.GetCode());
            return cb;
        }

        public CodeBuilder GetSourceCode()
        {
            if (EmitInline)
            {
                return new CodeBuilder();
            }

            bool isConst = this.get_IsConstant();
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();
            if (this.get_IsAbstract() || DeclaringType.get_IsInterface())
            {
                return cb;
            }

            var allTemplDefs = FullTemplateDefinition;
            cb.AddCodeBuilder(allTemplDefs.GetHeaderCode());
            if (!allTemplDefs.IsEmpty)
            {
                cb.AppendLine();
            }

            cb.Append(GetSharedSignature(!IsGlobal));
            cb.AddCodeBuilder(GetBodyCode());

            return cb;
        }

        public override string ToString()
        {
            return GetHeaderCode().ToString();
        }

        #endregion

        #region Equality

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ DeclaringType.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is IMethod)
            {
                return this.Equals((IMethod)obj);
            }
            else
            {
                return false;
            }
        }

        public static bool Equals(IMethod First, IMethod Other)
        {
            if (object.ReferenceEquals(First, Other))
            {
                return true;
            }
            else if (First == null || Other == null)
            {
                return false;
            }
            if ((First.DeclaringType == null || Other.DeclaringType == null) && object.ReferenceEquals(First.DeclaringType, Other.DeclaringType))
            {
                return false;
            }
            if (First.Name == Other.Name && Other.IsStatic == First.IsStatic && (First.DeclaringType == null || First.DeclaringType.Equals(Other.DeclaringType)))
            {
                var tComparer = new ScopedTypeEqualityComparer();

                if (!tComparer.Compare(Other.ReturnType, First.ReturnType))
                    return false;
                var otherParams = Other.GetParameters();
                var parameters = First.GetParameters();
                if (otherParams.Length != parameters.Length)
                    return false;

                for (int i = 0; i < otherParams.Length; i++)
                {
                    if (!tComparer.Compare(parameters[i].ParameterType, otherParams[i].ParameterType))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public bool Equals(IMethod Other)
        {
            return Equals(this, Other);
        }

        #endregion
    }
}
