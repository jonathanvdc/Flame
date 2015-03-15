using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilParameter : IParameter
    {
        public CecilParameter(ICecilTypeMember DeclaringMember, ParameterReference Parameter)
        {
            this.DeclaringMember = DeclaringMember;
            this.Parameter = Parameter;
        }

        public ParameterReference Parameter { get; private set; }
        public ICecilTypeMember DeclaringMember { get; private set; }

        public ParameterDefinition GetResolvedParameter()
        {
            return Parameter.Resolve();
        }

        protected IList<IAttribute> GetMemberAttributes(ParameterDefinition Parameter)
        {
            List<IAttribute> attrs = new List<IAttribute>();
            if (Parameter.IsOut)
            {
                attrs.Add(CecilAttribute.CreateCecil<System.Runtime.InteropServices.OutAttribute>(DeclaringMember));
            }
            return attrs;
        }

        protected IList<IAttribute> GetCustomAttributes(ParameterDefinition Parameter)
        {
            return CecilAttribute.GetAttributes(Parameter.CustomAttributes, DeclaringMember);
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            var resolvedParam = GetResolvedParameter();
            var memberAttrs = GetMemberAttributes(resolvedParam);
            var customAttrs = GetCustomAttributes(resolvedParam);
            var attrs = new IAttribute[memberAttrs.Count + customAttrs.Count];
            memberAttrs.CopyTo(attrs, 0);
            customAttrs.CopyTo(attrs, memberAttrs.Count);
            return attrs;
        }

        public string Name
        {
            get { return Parameter.Name; }
        }

        public string FullName
        {
            get
            {
                if (DeclaringMember is IMember)
                {
                    return MemberExtensions.CombineNames(((IMember)DeclaringMember).FullName, Parameter.Name);
                }
                else
                {
                    return Parameter.Name;
                }
            }
        }

        public IType ParameterType
        {
            get { return CecilTypeBase.Create(Parameter.ParameterType); }
        }

        public bool IsAssignable(IType Type)
        {
            return Type.Is(ParameterType);
        }

        #region Static

        public static IParameter[] GetParameters(ICecilTypeMember Resolver, IList<ParameterDefinition> Parameters)
        {
            IParameter[] cecilParams = new IParameter[Parameters.Count];
            for (int i = 0; i < cecilParams.Length; i++)
            {
                cecilParams[i] = new CecilParameter(Resolver, Parameters[i]);
            }
            return cecilParams;
        }

        private static CecilParameter DeclareParameter(ICecilTypeMember Member, IParameter Template, out ParameterDefinition paramDef)
        {
            var module = Member.GetModule();
            IType paramType = Member.ResolveType(Template.ParameterType);
            IGenericParameterProvider context;
            if (Member is IGenericMember)
            {
                paramType = CecilTypeBuilder.GetGenericType(paramType, ((IGenericMember)Member).GetGenericParameters().ToArray());
                context = Member.GetMemberReference() as IGenericParameterProvider;
            }
            else
	        {
                context = null;
	        }
            paramDef = new ParameterDefinition(Template.Name, ParameterAttributes.None, paramType.GetImportedReference(module, context));
            var cecilParam = new CecilParameter(Member, paramDef);
            var attrs = Template.GetAttributes();
            if (attrs.Any((item) => item.AttributeType.Equals(PrimitiveAttributes.Instance.OutAttribute.AttributeType)))
            {
                paramDef.IsOut = true;
            }
            CecilAttribute.DeclareAttributes(paramDef, Member, attrs.Where((item) => item.AttributeType.Equals(PrimitiveAttributes.Instance.OutAttribute.AttributeType)));
            return cecilParam;
        }

        public static CecilParameter DeclareParameter(CecilMethod Method, IParameter Template)
        {
            ParameterDefinition paramDef;
            var cecilParam = DeclareParameter(Method, Template, out paramDef);
            Method.Method.Parameters.Add(paramDef);
            return cecilParam;
        }
        public static CecilParameter DeclareParameter(CecilProperty Property, IParameter Template)
        {
            ParameterDefinition paramDef;
            var cecilParam = DeclareParameter(Property, Template, out paramDef);
            Property.GetResolvedProperty().Parameters.Add(paramDef);
            return cecilParam;
        }

        #endregion
    }
}
