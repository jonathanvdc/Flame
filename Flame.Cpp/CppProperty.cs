using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppProperty : IPropertyBuilder, ICppMember
    {
        public CppProperty(IType DeclaringType, IPropertySignatureTemplate Template, ICppEnvironment Environment)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new PropertySignatureInstance(Template, this);
            this.Environment = Environment;
            this.accessors = new List<CppAccessor>();
        }

        public IType DeclaringType { get; private set; }
        public PropertySignatureInstance Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        #region Accessors

        private List<CppAccessor> accessors;

        #endregion

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return this.PropertyType.GetDependencies().MergeDependencies(IndexerParameters.GetDependencies()).MergeDependencies(accessors.SelectMany((item) => item.Dependencies)); }
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

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public string Name
        {
            get 
            {
                if (this.GetIsIndexer())
                {
                    return "Item";
                }
                else
                {
                    return Template.Name; 
                }
            }
        }

        public IMethodBuilder DeclareAccessor(AccessorType Kind, IMethodSignatureTemplate Template)
        {
            var accessor = new CppAccessor(this, Kind, Template, Environment);
            accessors.Add(accessor);
            return accessor;
        }

        public IProperty Build()
        {
            return this;
        }

        public void Initialize()
        {
            // There's no need for initialization logic in this member.
        }

        public IEnumerable<IAccessor> Accessors
        {
            get { return accessors; }
        }

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
    }
}
