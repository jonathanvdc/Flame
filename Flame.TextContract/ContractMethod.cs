using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractMethod : IMethodBuilder, ISyntaxNode
    {
        public ContractMethod(IType DeclaringType, IMethodSignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new MethodSignatureInstance(Template, this);
        }

        public IType DeclaringType { get; private set; }
        public MethodSignatureInstance Template { get; private set; }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Template.BaseMethods.Value; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return Template.Parameters.Value; }
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
        }

        public IType ReturnType
        {
            get { return Template.ReturnType.Value; }
        }

        public bool IsStatic
        {
            get { return Template.Template.IsStatic; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public virtual string Name
        {
            get
            {
                if (this.IsConstructor)
                    return "Create" + DeclaringType.GetGenericFreeName();
                else if (this.GetIsGeneric())
                {
                    var genericFreeName = GenericNameExtensions.TrimGenerics(this.Template.Name);
                    StringBuilder sb = new StringBuilder(genericFreeName);
                    sb.Append('<');
                    bool first = true;
                    foreach (var item in GenericParameters)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        sb.Append(ContractHelpers.GetTypeName(item));
                    }
                    sb.Append('>');
                    return sb.ToString();
                }
                else
                {
                    return Template.Name;
                }
            }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters.Value; }
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append(ContractHelpers.GetAccessCode(this.GetAccess()));
            cb.Append(Name);
            cb.Append("(");
            bool first = true;
            foreach (var item in Parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cb.Append(", ");
                }
                if (item.HasAttribute(PrimitiveAttributes.Instance.OutAttribute.AttributeType))
                {
                    cb.Append("out ");
                }
                else
                {
                    cb.Append("in ");
                }
                cb.Append(item.Name);
                cb.Append(" : ");
                cb.Append(ContractHelpers.GetTypeName(item.ParameterType));
            }
            cb.Append(")");
            if (this.GetHasReturnValue())
            {
                cb.Append(" : ");
                cb.Append(ContractHelpers.GetTypeName(ReturnType));
            }
            else if (this.IsConstructor)
            {
                cb.Append(" : ");
                cb.Append(ContractHelpers.GetTypeName(DeclaringType));
            }
            var modifiers = ContractHelpers.GetModifiers(this);
            if (modifiers.Count > 0)
            {
                cb.Append(" { ");
                first = true;
                foreach (var item in modifiers)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        cb.Append(", ");
                    }
                    cb.Append(item);
                }
                cb.Append(" }");
            }
            cb.AddCodeBuilder(this.GetDocumentationCode());
            return cb;
        }

        public IMethod Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public ICodeGenerator GetBodyGenerator()
        {
            return null;
        }

        public void SetMethodBody(ICodeBlock Body)
        {

        }
    }
}
