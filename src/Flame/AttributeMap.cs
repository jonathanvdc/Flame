using System;
using System.Collections;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame
{
    /// <summary>
    /// Defines a mutable attribute map.
    /// </summary>
    public sealed class AttributeMapBuilder
    {
        /// <summary>
        /// Creates an empty attribute map builder.
        /// </summary>
        public AttributeMapBuilder()
        {
            attributeDict = new SmallMultiDictionary<IType, IAttribute>();
        }

        /// <summary>
        /// Creates an attribute map builder from a sequence of attributes.
        /// </summary>
        /// <param name="attributes">A sequence of attributes.</param>
        public AttributeMapBuilder(IEnumerable<IAttribute> attributes)
        {
            attributeDict = new SmallMultiDictionary<IType, IAttribute>();
            AddRange(attributes);
        }

        /// <summary>
        /// Creates an attribute map builder from an array of attributes.
        /// </summary>
        /// <param name="attributes">An array of attributes.</param>
        public AttributeMapBuilder(params IAttribute[] attributes)
        {
            attributeDict = new SmallMultiDictionary<IType, IAttribute>(attributes.Length);
            foreach (var item in attributes)
                Add(item);
        }

        /// <summary>
        /// Creates an attribute map builder by copying the contents
        /// of another attribute map builder.
        /// </summary>
        /// <param name="attributes">An attribute map builder.</param>
        public AttributeMapBuilder(AttributeMapBuilder attributes)
        {
            attributeDict = new SmallMultiDictionary<IType, IAttribute>(attributes.attributeDict);
        }

        /// <summary>
        /// Creates an attribute map builder by copying the contents
        /// of an existing attribute map.
        /// </summary>
        /// <param name="attributes">An attribute map.</param>
        public AttributeMapBuilder(AttributeMap attributes)
        {
            attributeDict = new SmallMultiDictionary<IType, IAttribute>(attributes.attributeDict);
        }

        internal SmallMultiDictionary<IType, IAttribute> attributeDict;

        /// <summary>
        /// Adds the given attribute to this attribute map builder.
        /// </summary>
        public void Add(IAttribute attribute)
        {
            // GetAttributeBag(Attribute.AttributeType).Add(Attribute);
            attributeDict.Add(attribute.AttributeType, attribute);
        }

        /// <summary>
        /// Adds the given range of attributes to this attribute map builder.
        /// </summary>
        public void AddRange(IEnumerable<IAttribute> attributes)
        {
            foreach (var item in attributes)
                Add(item);
        }

        /// <summary>
        /// Adds the given range of attributes to this attribute map builder.
        /// </summary>
        public void AddRange(IAttribute[] attributes)
        {
            attributeDict.Reserve(attributeDict.Count + attributes.Length);
            foreach (var item in attributes)
                Add(item);
        }

        /// <summary>
        /// Adds the given range of attributes to this attribute map builder.
        /// </summary>
        public void AddRange(AttributeMapBuilder attributes)
        {
            attributeDict.AddRange(attributes.attributeDict);
            // foreach (var kvPair in attributes.attributeDict)
            //     GetAttributeBag(kvPair.Key).AddRange(kvPair.Value);
        }

        /// <summary>
        /// Adds the given range of attributes to this attribute map builder.
        /// </summary>
        public void AddRange(AttributeMap attributes)
        {
            attributeDict.AddRange(attributes.attributeDict);
        }

        /// <summary>
        /// Removes all attributes that have the given type.
        /// </summary>
        public void RemoveAll(IType type)
        {
            attributeDict.Remove(type);
        }

        /// <summary>
        /// Gets all attributes of a particular type in this attribute map.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// A sequence that contains all attributes of type <paramref name="type"/>
        /// defined in this attribute map.
        /// </returns>
        public IEnumerable<IAttribute> GetAll(IType type)
        {
            return attributeDict.GetAll(type);
        }

        /// <summary>
        /// Finds the first attribute of a particular type. Returns <c>null</c>
        /// if no such attribute exists.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// An attribute of type <paramref name="type"/>, if one is defined in
        /// this attribute map; otherwise, <c>null</c>.
        /// </returns>
        public IAttribute GetOrNull(IType type)
        {
            return attributeDict.PeekOrDefault(type);
        }

        /// <summary>
        /// Checks if this attribute map contains at least one attribute
        /// of a particular type.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// <c>true</c> if an attribute of type <paramref name="type"/>
        /// is in this attribute map; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(IType type)
        {
            return attributeDict.ContainsKey(type);
        }
    }

    /// <summary>
    /// Defines a container that facilitates quick type-based attribute lookups.
    /// </summary>
    public struct AttributeMap
    {
        /// <summary>
        /// Creates a new attribute map from a sequence of attributes.
        /// </summary>
        /// <param name="attributes">
        /// A sequence of attributes to wrap in an attribute map.
        /// </param>
        public AttributeMap(IEnumerable<IAttribute> attributes)
        {
            this.attributeDict = new AttributeMapBuilder(attributes).attributeDict;
        }

        /// <summary>
        /// Creates a new attribute map from a sequence of attributes.
        /// </summary>
        /// <param name="attributes">
        /// A sequence of attributes to wrap in an attribute map.
        /// </param>
        public AttributeMap(params IAttribute[] attributes)
        {
            this.attributeDict = new AttributeMapBuilder(attributes).attributeDict;
        }

        /// <summary>
        /// Creates this attribute map as a read-only view of an
        /// attribute map builder.
        /// </summary>
        /// <param name="builder">
        /// An attribute map builder to create a read-only view of.
        /// </param>
        public AttributeMap(AttributeMapBuilder builder)
        {
            this.attributeDict = builder.attributeDict;
        }

        internal SmallMultiDictionary<IType, IAttribute> attributeDict;

        /// <summary>
        /// Gets all attributes in this attribute map.
        /// </summary>
        /// <returns>
        /// A sequence that contains all attributes in this attribute map.
        /// </returns>
        public IEnumerable<IAttribute> GetAll()
        {
            return attributeDict.Values;
        }

        /// <summary>
        /// Gets all attributes of a particular type in this attribute map.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// A sequence that contains all attributes of type <paramref name="type"/>
        /// defined in this attribute map.
        /// </returns>
        public IEnumerable<IAttribute> GetAll(IType type)
        {
            return attributeDict.GetAll(type);
        }

        /// <summary>
        /// Finds the first attribute of a particular type. Returns <c>null</c>
        /// if no such attribute exists.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// An attribute of type <paramref name="type"/>, if one is defined in
        /// this attribute map; otherwise, <c>null</c>.
        /// </returns>
        public IAttribute GetOrNull(IType type)
        {
            return attributeDict.PeekOrDefault(type);
        }

        /// <summary>
        /// Tries to find an attribute of a particular type.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <param name="attribute">
        /// A variable to store the attribute in, if one is found.
        /// </param>
        /// <returns>
        /// <c>true</c> if an attribute of type <paramref name="type"/>
        /// is found in this attribute map and stored in <paramref name="attribute"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool TryGet(IType type, out IAttribute attribute)
        {
            attribute = GetOrNull(type);
            return attribute != null;
        }

        /// <summary>
        /// Checks if this attribute map contains at least one attribute
        /// of a particular type.
        /// </summary>
        /// <param name="type">The type of attribute to look for.</param>
        /// <returns>
        /// <c>true</c> if an attribute of type <paramref name="type"/>
        /// is in this attribute map; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(IType type)
        {
            return attributeDict.ContainsKey(type);
        }

        /// <summary>
        /// An empty attribute map.
        /// </summary>
        /// <value>An attribute map.</value>
        public static readonly AttributeMap Empty = new AttributeMap(new IAttribute[] { });
    }
}
