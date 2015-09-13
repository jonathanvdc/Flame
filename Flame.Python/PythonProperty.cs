using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonProperty : IPropertyBuilder, IPythonProperty, ISyntaxNode
    {
        public PythonProperty(IType DeclaringType, IPropertySignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new PropertySignatureInstance(Template, this);
            this.Accessors = new List<IPythonAccessor>();
        }

        public IType DeclaringType { get; private set; }
        public PropertySignatureInstance Template { get; private set; }
        public List<IPythonAccessor> Accessors { get; private set; }

        public IEnumerable<IParameter> IndexerParameters
        {
            get { return Template.IndexerParameters.Value; }
        }

        public IType PropertyType
        {
            get { return Template.PropertyType.Value; }
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

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringType.DeclaringNamespace.GetMemberNamer();
        }

        public string Name
        {
            get
            {
                var descProp = new DescribedProperty(Template.Name, DeclaringType);
                return GetMemberNamer().Name(descProp);
            }
        }

        public IMethodBuilder DeclareAccessor(AccessorType Type, IMethodSignatureTemplate Template)
        {
            var accessor = new PythonAccessor(this, Type, Template);
            this.Accessors.Add(accessor);
            return accessor;
        }

        public IProperty Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            CodeBuilder otherAccessors = new CodeBuilder();
            foreach (var item in Accessors)
            {
                if (item.AccessorType.Equals(AccessorType.GetAccessor)) // Make sure the getter comes first for property decorator reasons
                {
                    cb.AddCodeBuilder(item.GetCode());
                    cb.AddEmptyLine();
                }
                else
                {
                    otherAccessors.AddCodeBuilder(item.GetCode());
                    otherAccessors.AddEmptyLine();
                }
            }
            cb.AddCodeBuilder(otherAccessors);
            return cb;
        }

        #region IPythonProperty Implementation

        public bool UsesPropertySyntax
        {
            get
            {
                // All Python properties have getters, so that's definitely a requirement.
                // Also, indexers get special treatment, so they shouldn't be included here.
                // In addition, property names should not overlap with attribute names, because that's common decency.
                return DeclaringType.GetField(Name) == null && !this.get_IsIndexer() && Accessors.Any((item) => item.AccessorType.Equals(AccessorType.GetAccessor));
            }
        }

        #endregion

        IEnumerable<IAccessor> IProperty.Accessors
        {
            get { return Accessors; }
        }
    }
}
