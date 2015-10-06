using Flame.Attributes;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    /// <summary>
    /// A number of functions that facilitate parsing
    /// Flame IR attributes.
    /// </summary>
    public static class AttributeParsers
    {
        #region Constants

        public const string PublicNodeName = "#public";
        public const string PrivateNodeName = "#private";
        public const string ProtectedNodeName = "#protected";
        public const string InternalNodeName = "#internal";
        public const string ProtectedOrInternal = "#protected_or_internal";
        public const string ProtectedAndInternal = "#protected_and_internal";

        public const string ConstantNodeName = "#const";
        public const string ExtensionNodeName = "#extension";
        public const string HiddenNodeName = "#hidden";

        public const string AbstractNodeName = "#abstract";
        public const string VirtualNodeName = "#virtual";

        public const string StaticTypeNodeName = "#static_type";
        public const string ReferenceTypeNodeName = "#reference_type";
        public const string ValueTypeNodeName = "#value_type";
        public const string InterfaceTypeNodeName = "#interface_type";

        public const string ConstructedAttributeNodeName = "#attribute";

        #endregion

        #region Parsing methods

        /// <summary>
        /// Creates an attribute "parser" that 
        /// simply returns a wrapped instance of the
        /// given attribute value.
        /// </summary>
        /// <param name="Attribute"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IAttribute>> CreateConstantAttributeParser(IAttribute Attribute)
        {
            return new Func<ParserState, LNode, INodeStructure<IAttribute>>((state, node) =>
                new ConstantNodeStructure<IAttribute>(node, Attribute));
        }

        /// <summary>
        /// Parses a constructed attribute:
        /// an attribute instance that is constructed
        /// by calling an attribute constructor 
        /// on a number of compile-time constant expressions.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static INodeStructure<IAttribute> ParseConstructedAttribute(ParserState State, LNode Node)
        {
            if (Node.ArgCount == 0)
            {
                throw new InvalidOperationException("Invalid '" + ConstructedAttributeNodeName + "' node: '" +
                    ConstructedAttributeNodeName + "' nodes must have at least one argument, representing the attribute's constructor.");
            }

            // Format: #attribute(<attribute_constructor>, <argument_expressions...>)
            return new LazyNodeStructure<IAttribute>(Node, () =>
            {
                var attrCtor = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
                var args = ExpressionParsers.ParseExpressions(State, Node.Args.Skip(1))
                               .Select(item => item.Evaluate())
                               .ToArray();

                if (args.Any(item => item == null))
                {
                    throw new InvalidOperationException("Invalid '" + ConstructedAttributeNodeName + 
                        "' node: one of the attribute node's arguments could be evaluated at compile-time.");
                }

                int paramCount = attrCtor.Parameters.Count();
                if (args.Length != paramCount)
                {
                    throw new InvalidOperationException("Invalid '" + ConstructedAttributeNodeName + "' node: '" +
                        attrCtor.FullName + "' takes " + paramCount + (paramCount == 1 ? " argument" : " arguments") + ", but is given " +
                        args.Length + ".");
                }
                return new ConstructedAttribute(attrCtor, args);
            });
        }

        #endregion

        #region Default parser

        public static ReferenceParser<IAttribute> DefaultAttributeParser
        {
            get
            {
                return new ReferenceParser<IAttribute>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IAttribute>>>()
                {
                    // Constructed attributes:
                    { ConstructedAttributeNodeName, ParseConstructedAttribute },

                    // Primitive attributes:
                    // Access attributes:
                    { PublicNodeName, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.Public)) },
                    { PrivateNodeName, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.Private)) },
                    { ProtectedNodeName, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.Protected)) },
                    { InternalNodeName, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.Assembly)) },
                    { ProtectedOrInternal, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.ProtectedOrAssembly)) },
                    { ProtectedAndInternal, CreateConstantAttributeParser(new AccessAttribute(AccessModifier.ProtectedAndAssembly)) },

                    // Miscellaneous attributes
                    { ConstantNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ConstantAttribute) },
                    { ExtensionNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ExtensionAttribute) },
                    { HiddenNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.HiddenAttribute) },

                    // Inheritance attributes
                    { AbstractNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.AbstractAttribute) },
                    { VirtualNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.VirtualAttribute) },

                    // Type attributes
                    { StaticTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.StaticTypeAttribute) },
                    { ReferenceTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ReferenceTypeAttribute) },
                    { ValueTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ValueTypeAttribute) },
                    { InterfaceTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.InterfaceAttribute) }
                });
            }
        }

        #endregion
    }
}
