using Flame.Compiler;
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
        public ContractMethod(IType DeclaringType, IMethod Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
        }

        public IType DeclaringType { get; private set; }
        public IMethod Template { get; private set; }

        public IMethod[] GetBaseMethods()
        {
            return Template.GetBaseMethods();
        }

        public IMethod GetGenericDeclaration()
        {
            return new ContractMethod(DeclaringType, Template.GetGenericDeclaration());
        }

        public IParameter[] GetParameters()
        {
            return Template.GetParameters();
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return Template.Invoke(Caller, Arguments);
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new ContractMethod(DeclaringType, Template.MakeGenericMethod(TypeArguments));
        }

        public IType ReturnType
        {
            get { return Template.ReturnType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }

        public string FullName
        {
            get { return Template.FullName; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public virtual string Name
        {
            get
            {
                if (this.IsConstructor)
                    return "Create" + DeclaringType.GetGenericFreeName();
                else if (this.get_IsGeneric())
                {
                    var genericFreeName = this.Template.GetGenericDeclaration().GetGenericFreeName();
                    StringBuilder sb = new StringBuilder(genericFreeName);
                    sb.Append('<');
                    bool first = true;
                    foreach (var item in GetGenericParameters())
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

        public IEnumerable<IType> GetGenericArguments()
        {
            return Template.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Template.GetGenericParameters();
        }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.Append(ContractHelpers.GetAccessCode(this.get_Access()));
            cb.Append(Name);
            cb.Append("(");
            bool first = true;
            foreach (var item in GetParameters())
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
            if (this.get_HasReturnValue())
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

        public IBlockGenerator GetBodyGenerator()
        {
            return null;
        }

        public IMethod Build()
        {
            return this;
        }
    }
}
