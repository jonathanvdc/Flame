using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public abstract class PrimitiveBase : IType
    {
        public PrimitiveBase()
        {
            this.cachedFields = new Lazy<IField[]>(CreateFields);
            this.cachedMethods = new Lazy<IMethod[]>(CreateMethods);
            this.cachedProperties = new Lazy<IProperty[]>(CreateProperties);
        }

        #region Abstract

        public abstract UnqualifiedName Name { get; }
        public abstract AttributeMap Attributes { get; }
        public abstract INamespace DeclaringNamespace { get; }

        #endregion

        #region General

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public virtual IEnumerable<IType> BaseTypes
        {
            get { return new IType[0]; }
        }

        public virtual IBoundObject GetDefaultValue()
        {
            return null;
        }

        #endregion

        #region Members

        protected virtual IField[] CreateFields()
        {
            return new IField[0];
        }
        protected virtual IMethod[] CreateMethods()
        {
            return new IMethod[0];
        }
        protected virtual IProperty[] CreateProperties()
        {
            return new IProperty[0];
        }

        private Lazy<IField[]> cachedFields;
        private Lazy<IMethod[]> cachedMethods;
        private Lazy<IProperty[]> cachedProperties;

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        public IEnumerable<IField> Fields
        {
            get { return cachedFields.Value; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return cachedMethods.Value; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return cachedProperties.Value; }
        }

        #endregion

        #region Generics

        public virtual IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        #endregion
    }
}
