using Flame.Compiler;
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
        public PythonProperty(IType DeclaringType, IProperty Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.Accessors = new List<IPythonAccessor>();
        }

        public IType DeclaringType { get; private set; }
        public IProperty Template { get; private set; }
        public List<IPythonAccessor> Accessors { get; private set; }

        public IAccessor[] GetAccessors()
        {
            return Accessors.ToArray();
        }

        public IParameter[] GetIndexerParameters()
        {
            return Template.IndexerParameters;
        }

        public IType PropertyType
        {
            get { return Template.PropertyType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.Attributes;
        }

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringType.DeclaringNamespace.GetMemberNamer();
        }

        public string Name
        {
            get { return GetMemberNamer().Name(Template); }
        }

        public IMethodBuilder DeclareAccessor(IAccessor Template)
        {
            var accessor = new PythonAccessor(this, Template);
            this.Accessors.Add(accessor);
            return accessor;
        }

        public IProperty Build()
        {
            return this;
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
                return DeclaringType.GetField(Name) == null && !this.get_IsIndexer() && GetAccessors().Any((item) => item.AccessorType.Equals(AccessorType.GetAccessor));
            }
        }

        #endregion
    }
}
