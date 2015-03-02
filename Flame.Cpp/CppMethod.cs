using Flame.Build;
using Flame.Compiler;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppMethod : IMethodBuilder, ICppTemplateMember
    {
        public CppMethod(IGenericResolverType DeclaringType, IMethod Template, ICppEnvironment Environment)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.Templates = new CppTemplateDefinition(this, Template);
            this.Environment = new TemplatedMemberCppEnvironment(Environment, this);
            var codeGen = new CppCodeGenerator(this, this.Environment);
            this.blockGen = new CppContractBlockGenerator(codeGen, codeGen.Contract);
            this.built = false;
        }

        public IType DeclaringType { get; private set; }
        public IMethod Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }
        public CppTemplateDefinition Templates { get; private set; }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return ReturnType.GetDependencies(DeclaringType).MergeDependencies(GetParameters().GetDependencies(DeclaringType)).MergeDependencies(Body.Dependencies); }
        }

        private bool built;
        private CppContractBlockGenerator blockGen;
        public IBlockGenerator GetBodyGenerator()
        {
            if (built)
            {
                throw new InvalidOperationException();
            }
            return blockGen;
        }

        protected CppBlockGenerator Body
        {
            get { return blockGen; }
        }

        public IMethod Build()
        {
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

        public bool IsVirtual
        {
            get { return this.get_IsVirtual() || this.IsPureVirtual; }
        }

        public bool IsPureVirtual
        {
            get { return this.get_IsAbstract() || this.DeclaringType.get_IsInterface(); }
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

        public bool IsStatic
        {
            get { return Template.IsStatic; }
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
            bool isConst = this.get_IsConstant();
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
                if (isConst && tParam.get_IsPointer() && tParam.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.TransientPointer))
                {
                    cb.Append("const ");
                }
                cb.Append(TypeNamer.Name(tParam, this));
                cb.Append(' ');
                cb.Append(parameters[i].Name);
            }
            cb.Append(')');
            if (isConst && !IsExistential)
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
            cb.AddCodeBuilder(Templates.GetHeaderCode());
            cb.AppendLine();
            if (this.IsStatic)
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
            cb.Append(';');
            return cb;
        }

        public bool HasSourceCode
        {
            get { return !IsPureVirtual; }
        }

        public CodeBuilder GetSourceCode()
        {
            var cg = blockGen.CodeGenerator;

            bool isConst = this.get_IsConstant();
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();
            if (this.get_IsAbstract() || DeclaringType.get_IsInterface())
            {
                return cb;
            }

            var declTemplDefs = DeclaringType.GetTemplateDefinition();
            var allTemplDefs = declTemplDefs.Merge(Templates);
            cb.AddCodeBuilder(allTemplDefs.GetHeaderCode());
            if (!allTemplDefs.IsEmpty)
            {
                cb.AppendLine();
            }

            cb.Append(GetSharedSignature(true));

            var body = blockGen.ImplyEmptyReturns();
            if (this.IsConstructor)
            {
                body = body.ImplyStructInit();
            }
            cb.AddEmbracedBodyCodeBuilder(body.GetCode());

            return cb;
        }

        public override string ToString()
        {
            return GetHeaderCode().ToString();
        }

        #endregion
    }
}
