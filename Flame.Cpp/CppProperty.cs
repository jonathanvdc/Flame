using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppProperty : IPropertyBuilder, ICppMember
    {
        public CppProperty(IType DeclaringType, IProperty Template, ICppEnvironment Environment)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.Environment = Environment;
            this.accessors = new List<CppAccessor>();
        }

        public IType DeclaringType { get; private set; }
        public IProperty Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        #region Accessors

        private List<CppAccessor> accessors;

        #endregion

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return this.PropertyType.GetDependencies().MergeDependencies(GetIndexerParameters().GetDependencies()).MergeDependencies(accessors.SelectMany((item) => item.Dependencies)); }
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in accessors)
            {
                cb.AddCodeBuilder(item.GetHeaderCode());
            }
            return cb;
        }

        public bool HasSourceCode
        {
            get { return accessors.Any((item) => item.HasSourceCode); }
        }

        public CodeBuilder GetSourceCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in accessors)
            {
                cb.AddCodeBuilder(item.GetSourceCode());
            }
            return cb;
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public string Name
        {
            get 
            {
                if (this.get_IsIndexer())
                {
                    return "Item";
                }
                else
                {
                    return Template.Name; 
                }
            }
        }

        public IMethodBuilder DeclareAccessor(IAccessor Template)
        {
            var accessor = new CppAccessor(this, Template, Environment);
            accessors.Add(accessor);
            return accessor;
        }

        public IProperty Build()
        {
            return this;
        }

        public IAccessor[] GetAccessors()
        {
            return accessors.ToArray();
        }

        public IParameter[] GetIndexerParameters()
        {
            return Template.GetIndexerParameters();
        }

        public IType PropertyType
        {
            get { return Template.PropertyType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }
    }
}
