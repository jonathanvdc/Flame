using System;
using Loyc.Syntax;

namespace Flame.Ir
{
    /// <summary>
    /// A base class for members that are decoded from LNodes.
    /// </summary>
    public abstract class IrMember : IMember
    {
        /// <summary>
        /// Creates a member that is the decoded version of a node.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="decoder">The decoder to use.</param>
        public IrMember(LNode node, DecoderState decoder)
        {
            this.Node = node;
            this.Decoder = decoder;
            this.attributeCache = new Lazy<AttributeMap>(() =>
                Decoder.DecodeAttributeMap(Node.Attrs));
            this.nameCache = new Lazy<QualifiedName>(() =>
                QualifyName(Decoder.DecodeQualifiedName(Node.Args[0])));
        }

        /// <summary>
        /// Gets the encoded version of this member.
        /// </summary>
        /// <returns>The encoded version.</returns>
        public LNode Node { get; private set; }

        /// <summary>
        /// Gets the decoder that is used for decoding this member.
        /// </summary>
        /// <returns>The decoder.</returns>
        public DecoderState Decoder { get; private set; }

        private Lazy<AttributeMap> attributeCache;
        private Lazy<QualifiedName> nameCache;

        /// <summary>
        /// Qualifies this member's name.
        /// </summary>
        /// <param name="name">The name to qualify.</param>
        /// <returns>The qualified name.</returns>
        protected virtual QualifiedName QualifyName(QualifiedName name)
        {
            var scope = Decoder.Scope;
            if (scope.IsType)
            {
                return name.Qualify(scope.Type.FullName);
            }
            else if (scope.IsMethod)
            {
                return name.Qualify(scope.Method.FullName);
            }
            else
            {
                return name;
            }
        }

        /// <summary>
        /// Gets this member's full name.
        /// </summary>
        /// <returns>The full name.</returns>
        public QualifiedName FullName => nameCache.Value;

        /// <summary>
        /// Gets this member's unqualified name.
        /// </summary>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <summary>
        /// Gets this member's attributes.
        /// </summary>
        public AttributeMap Attributes => attributeCache.Value;
    }
}
