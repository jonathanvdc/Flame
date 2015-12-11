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
    public class CppBinaryAssignmentOverload : ICppMethod
    {
        public CppBinaryAssignmentOverload(CppType DeclaringType, IMethod BinaryOverload)
        {
            this.DeclaringType = DeclaringType;
            this.BinaryOverload = BinaryOverload;
            this.Templates = new CppTemplateDefinition(this);
        }

        public CppType DeclaringType { get; private set; }
        public IMethod BinaryOverload { get; private set; }
        public CppTemplateDefinition Templates { get; private set; }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return new IMethod[] { }; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return BinaryOverload.IsStatic ? BinaryOverload.Parameters.Skip(1).ToArray() : BinaryOverload.Parameters; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IType ReturnType
        {
            get { return DeclaringType.MakeGenericType(DeclaringType.GenericParameters).MakePointerType(CppPointerExtensions.AtAddressPointer); }
        }

        public bool IsStatic
        {
            get { return false; }
        }

        public string FullName
        {
            get { return BinaryOverload.FullName + "="; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get
            {
                return new IAttribute[] 
                { 
                    new AccessAttribute(AccessModifier.Public), 
                    new OperatorAttribute(Operator.Register(BinaryOverload.GetOperator().Name + "=")) 
                };
            }
        }

        public string Name
        {
            get { return BinaryOverload.Name + "="; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        IType ITypeMember.DeclaringType
        {
            get { return DeclaringType; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[] { }; }
        }

        public ICppEnvironment Environment
        {
            get { return DeclaringType.Environment; }
        }

        #region GetCode

        #region GetSharedSignature

        public CodeBuilder GetDocumentationComments()
        {
            var envBuilder = Environment.DocumentationBuilder;
            var provider = new ConstantDocumentationProvider(Environment.DocumentationBuilder.Provider.GetDescriptionAttributes(BinaryOverload));
            var docBuilder = new DocumentationCommentBuilder(provider, envBuilder);
            return docBuilder.GetDocumentationComments(this);
        }

        private CodeBuilder GetSharedSignature(bool PrefixName)
        {
            CodeBuilder cb = new CodeBuilder();
            var envir = Environment;
            cb.Append(envir.TypeNamer.Name(ReturnType, this));
            cb.Append(' ');
            if (PrefixName)
            {
                var genDeclType = DeclaringType.MakeGenericType(DeclaringType.GenericParameters);
                cb.Append(envir.TypeNamer.Name(genDeclType, this));
                cb.Append("::");
            }
            cb.Append(this.Name);
            cb.Append('(');
            var parameters = this.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                var tParam = parameters[i].ParameterType;
                cb.Append(envir.TypeNamer.Name(tParam, this));
                cb.Append(' ');
                cb.Append(parameters[i].Name);
            }
            cb.Append(')');
            return cb;
        }

        #endregion

        public CodeBuilder GetHeaderCode()
        {
            bool isConst = this.GetIsConstant();
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();
            cb.Append(GetSharedSignature(false));
            cb.Append(';');
            return cb;
        }

        public bool HasSourceCode
        {
            get { return true; }
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
            var cb = new CodeBuilder();
            cb.AddLine("{");
            cb.IncreaseIndentation();
            cb.AddLine();
            cb.Append("return *this = *this ");
            cb.Append(Emit.BinaryOperation.GetOperatorString(BinaryOverload.GetOperator()));
            cb.Append(" ");
            cb.Append(this.GetParameters().Single().Name);
            cb.Append(";");
            cb.DecreaseIndentation();
            cb.AddLine("}");
            return cb;
        }

        public CodeBuilder GetSourceCode()
        {
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();

            var allTemplDefs = FullTemplateDefinition;
            cb.AddCodeBuilder(allTemplDefs.GetHeaderCode());
            if (!allTemplDefs.IsEmpty)
            {
                cb.AppendLine();
            }

            cb.Append(GetSharedSignature(true));
            cb.AddCodeBuilder(GetBodyCode());

            return cb;
        }

        public override string ToString()
        {
            return GetHeaderCode().ToString();
        }

        #endregion

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }
    }
}
