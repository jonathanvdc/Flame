using Flame.Build;
using Flame.CodeDescription;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        public CppMethod(IGenericResolverType DeclaringType, IMethodSignatureTemplate Template, ICppEnvironment Environment, bool IsStatic, bool IsGlobal)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new MethodSignatureInstance(Template, this);
            this.Templates = new CppTemplateDefinition(this, this.Template.GenericParameters);
            this.Environment = new TemplatedMemberCppEnvironment(Environment, this);
            this.IsStatic = IsStatic;
            this.IsGlobal = IsGlobal;
            this.codeGen = new CppCodeGenerator(this, this.Environment);
            this.built = false;
        }
        public CppMethod(IGenericResolverType DeclaringType, IMethodSignatureTemplate Template, ICppEnvironment Environment)
            : this(DeclaringType, Template, Environment, Template.IsStatic, false)
        { }

        public IType DeclaringType { get; private set; }
        public MethodSignatureInstance Template { get; private set; }
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
            get { return ReturnType.GetDependencies(DeclaringType).MergeDependencies(Parameters.GetDependencies(DeclaringType)).MergeDependencies(Body.Dependencies); }
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

        private ICppBlock processedBody;
        private ContractBlock methodBody;
        public ICppBlock Body { get { return methodBody as ICppBlock ?? new EmptyBlock(codeGen); } }

        public ICppBlock ProcessedBody
        {
            get
            {
                if (processedBody == null)
                {
                    processedBody = this.IsConstructor ? (ICppBlock)Body.OptimizeConstructorBody() : (ICppBlock)Body.OptimizeMethodBody();
                }
                return processedBody;
            }
        }

        public MethodContract Contract
        {
            get
            {
                return methodBody != null ? methodBody.Contract : new MethodContract(codeGen, Enumerable.Empty<ICppBlock>(), Enumerable.Empty<ICppBlock>());
            }
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.processedBody = null;
            this.methodBody = Body is ContractBlock ? (ContractBlock)Body : new ContractBlock((ICppBlock)Body, Enumerable.Empty<ICppBlock>(), Enumerable.Empty<ICppBlock>());
        }

        #endregion

        public IMethod Build()
        {
            built = true;
            return this;
        }

        public void Initialize()
        {
            // No need for this quite yet.
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public CodeBuilder GetDocumentationComments()
        {
            var envBuilder = Environment.DocumentationBuilder;
            var provider = new ConcatDocumentationProvider(envBuilder.Provider, new ConstantDocumentationProvider(Contract.DescriptionAttributes));
            var docBuilder = new DocumentationCommentBuilder(provider, envBuilder);
            return docBuilder.GetDocumentationComments(this);
        }

        public virtual UnqualifiedName Name
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

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Template.BaseMethods.Value; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get
            {
                return Template.Parameters.Value.Select(item => new RetypedParameter(item, Environment.TypeConverter.Convert(item.ParameterType)));
            }
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
            get { return this.GetIsCast(); }
        }

        public bool IsOperator
        {
            get { return this.GetIsOperator() || this.IsCast; }
        }

        public bool IsHashOperator
        {
            get { return this.GetIsOperator() && this.GetOperator().Equals(Operator.Hash); }
        }

        public bool IsVirtual
        {
            get { return this.GetIsVirtual() || this.IsPureVirtual; }
        }

        public bool IsPureVirtual
        {
            get { return this.GetIsAbstract() || this.DeclaringType.GetIsInterface(); }
        }

        public bool EmitInline
        {
            get { return (this.GetIsGeneric() && this.DeclaringType.GetIsGenericDeclaration()); }
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
            get { return BaseMethods.Any(); }
        }

        public IType ReturnType
        {
            get
            {
                return this.ConvertType(Template.ReturnType.Value);
            }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return this.Templates.GetGenericParameters(); }
        }

        #region GetCode

        #region GetSharedSignature

        private CodeBuilder GetSharedSignature(bool PrefixName)
        {
            bool isConst = this.GetIsConstant() && !IsExistential;
            bool isCast = IsCast;
            CodeBuilder cb = new CodeBuilder();
            if (!IsExistential && !isCast)
            {
                cb.Append(TypeNamer.Name(ReturnType, this));
                cb.Append(' ');
            }
            if (PrefixName)
            {
                var genDeclType = DeclaringType.MakeGenericType(DeclaringType.GenericParameters);
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
            var parameters = Parameters.ToArray();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                var tParam = parameters[i].ParameterType;
                if (isConst && tParam.GetIsPointer() && tParam.AsContainerType().AsPointerType().IsPrimitivePointer())
                {
                    cb.Append("const ");
                }
                cb.Append(TypeNamer.Name(tParam, this));
                cb.Append(' ');
                cb.Append(parameters[i].Name.ToString());
            }
            cb.Append(')');
            if (isConst && !IsStatic)
            {
                cb.Append(" const");
            }
            return cb;
        }

        #endregion

        public CodeBuilder GetHeaderCode()
        {
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
                cb.AddCodeBuilder(ProcessedBody.GetCode());
            }
            else
            {
                cb.Append(';');
                if (HasPublicBody) // Emit a sneak peek nonetheless
                {
                    var bodyCode = ProcessedBody.GetCode();
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

        public CodeBuilder GetSourceCode()
        {
            if (EmitInline)
            {
                return new CodeBuilder();
            }

            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();
            if (this.GetIsAbstract() || DeclaringType.GetIsInterface())
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
            cb.AddCodeBuilder(ProcessedBody.GetCode());

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
                var tComparer = ScopedTypeEqualityComparer.Instance;

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
