using System;
using Flame.Build;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A base class for members that are constructed lazily.
    /// </summary>
    public abstract class LazyDescribedMember : IMember
    {
        /// <summary>
        /// Initializes this lazily described member with the given name.
        /// </summary>
        public LazyDescribedMember(UnqualifiedName Name)
        {
            this.Name = Name;
            this.attributeList = new AttributeMapBuilder();
        }

        /// <summary>
        /// Gets this member's unqualified name.
        /// </summary>
        public UnqualifiedName Name { get; private set; }

        /// <summary>
        /// Gets this member's qualified name.
        /// </summary>
        public abstract QualifiedName FullName { get; }

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
        protected abstract void CreateBody();

        private AttributeMapBuilder attributeList;

        /// <summary>
        /// Adds the given attribute to this member's attribute map.
        /// </summary>
        public void AddAttribute(IAttribute Attribute)
        {
            CreateBody();
            attributeList.Add(Attribute);
        }

        /// <summary>
        /// Adds the given sequence of attributes to this member's attributes map.
        /// </summary>
        public void AddAttributes(IEnumerable<IAttribute> Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Adds the given attribute map to this member's attributes map.
        /// </summary>
        public void AddAttributes(AttributeMap Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Adds the given attribute map builder to this member's attributes map.
        /// </summary>
        public void AddAttributes(AttributeMapBuilder Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Removes all attributes of the given type from
        /// the attribute map.
        /// </summary>
        /// <param name="AttributeType">
        /// The attribute type whose instances will be removed
        /// from this attribute map.
        /// </param>
        public void RemoveAttributes(IType AttributeType)
        {
            CreateBody();
            attributeList.RemoveAll(AttributeType);
        }

        /// <summary>
        /// Gets this member's attribute map.
        /// </summary>
        public AttributeMap Attributes
        {
            get
            {
                CreateBody();
                return new AttributeMap(attributeList);
            }
        }

        /// <summary>
        /// Creates a string that identifies this member.
        /// </summary>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }
}