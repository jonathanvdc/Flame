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
            this.attrMap = new Lazy<AttributeMap>(CreateAttributes);
        }

        public ParameterReference Parameter { get; private set; }
        public ICecilTypeMember DeclaringMember { get; private set; }
        public CecilModule Module { get { return DeclaringMember.Module; } }

        public ParameterDefinition GetResolvedParameter()
        {
            return Parameter.Resolve();
        }

        protected List<IAttribute> GetMemberAttributes(ParameterDefinition Parameter)
        {
            List<IAttribute> attrs = new List<IAttribute>();
            if (Parameter.IsOut)
            {
                attrs.Add(CecilAttribute.CreateCecil<System.Runtime.InteropServices.OutAttribute>(DeclaringMember));
            }
            return attrs;
        }

        protected AttributeMap GetCustomAttributes(ParameterDefinition Parameter)
        {
            return CecilAttribute.GetAttributes(Parameter.CustomAttributes, DeclaringMember);
        }

        private Lazy<AttributeMap> attrMap;
        public AttributeMap Attributes
        {
            get
            {
                return attrMap.Value;
            }
        }

        private AttributeMap CreateAttributes()
        {
            var resolvedParam = GetResolvedParameter();
            var results = new AttributeMapBuilder();
            results.AddRange(GetMemberAttributes(resolvedParam));
            results.AddRange(GetCustomAttributes(resolvedParam));
            return new AttributeMap(results);
        }

        public UnqualifiedName Name
        {
            get { return new SimpleName(Parameter.Name); }
        }

        public QualifiedName FullName
        {
            get
            {
                if (DeclaringMember is IMember)
                {
                    return Name.Qualify(((IMember)DeclaringMember).FullName);
                }
                else
                {
                    return new QualifiedName(Name);
                }
            }
        }

        public IType ParameterType
        {
            get { return Module.Convert(Parameter.ParameterType); }
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
            var module = Member.Module;
            var context = Member.GetMemberReference() as IGenericParameterProvider;
            paramDef = new ParameterDefinition(Template.Name.ToString(), ParameterAttributes.None, Template.ParameterType.GetImportedReference(module, context));
            var cecilParam = new CecilParameter(Member, paramDef);
            var attrs = Template.Attributes;
            if (attrs.Any((item) => item.AttributeType.Equals(PrimitiveAttributes.Instance.OutAttribute.AttributeType)))
            {
                paramDef.IsOut = true;
            }
            CecilAttribute.DeclareAttributes(paramDef, Member, attrs.Where(item => item.AttributeType.Equals(PrimitiveAttributes.Instance.OutAttribute.AttributeType)));
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
