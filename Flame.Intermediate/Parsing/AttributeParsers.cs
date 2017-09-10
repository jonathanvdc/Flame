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
        public const string InNodeName = "#in";
        public const string OutNodeName = "#out";
        public const string IndexerNodeName = "#indexer";
        public const string ImportNodeName = "#import";
        public const string InitOnlyNodeName = "#init_only";
        public const string TotalInitializationNodeName = "#total_init";
        public const string InlineNodeName = "#inline";
        public const string VarArgsNodeName = "#varargs";

        public const string AbstractNodeName = "#abstract";
        public const string VirtualNodeName = "#virtual";

        public const string StaticTypeNodeName = "#static_type";
        public const string ReferenceTypeNodeName = "#reference_type";
        public const string ValueTypeNodeName = "#value_type";
        public const string InterfaceTypeNodeName = "#interface_type";
        public const string EnumTypeNodeName = "#enum_type";

        public const string AssociatedTypeNodeName = "#associated_type";
        public const string SingletonNodeName = "#singleton";
        public const string OperatorNodeName = "#operator";
        public const string DocumentationNodeName = "#docs";

        public const string IntrinsicAttributeNodeName = "#intrinsic";
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
        /// Parses the given '#singleton' attribute.
        /// </summary>
        public static INodeStructure<IAttribute> ParseSingletonAttribute(ParserState State, LNode Node)
        {
            return new ConstantNodeStructure<IAttribute>(
                Node, new SingletonAttribute(IRParser.GetIdOrString(Node.Args.Single())));
        }

        /// <summary>
        /// Parses the given '#associated_type' attribute.
        /// </summary>
        public static INodeStructure<IAttribute> ParseAssociatedTypeAttribute(ParserState State, LNode Node)
        {
            return new LazyValueStructure<IAttribute>(
                Node, () => new AssociatedTypeAttribute(State.Parser.TypeReferenceParser.Parse(State, Node.Args.Single()).Value));
        }

        /// <summary>
        /// Parses the given '#operator' attribute.
        /// </summary>
        public static INodeStructure<IAttribute> ParseOperatorAttribute(ParserState State, LNode Node)
        {
            return new LazyValueStructure<IAttribute>(
                Node, () => new OperatorAttribute(Operator.GetOperator(IRParser.GetIdOrString(Node.Args.Single()))));
        }

        /// <summary>
        /// Parses the given '#docs' attribute.
        /// </summary>
        public static INodeStructure<IAttribute> ParseDocumentationAttribute(ParserState State, LNode Node)
        {
            return new LazyValueStructure<IAttribute>(
                Node, () => new DescriptionAttribute(MarkupHelpers.Deserialize(Node.Args.Single())));
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
                throw new InvalidOperationException(
                    "Invalid '" + ConstructedAttributeNodeName + "' node: '" +
                    ConstructedAttributeNodeName +
                    "' nodes must have at least one argument, representing the attribute's constructor.");
            }

            // Format: #attribute(<attribute_constructor>, <argument_expressions...>)
            return new LazyValueStructure<IAttribute>(Node, () =>
            {
                var attrCtor = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
                var args = ExpressionParsers.ParseExpressions(State, Node.Args.Skip(1))
                               .Select(item => item.Evaluate())
                               .ToArray();

                if (args.Any(item => item == null))
                {
                    throw new InvalidOperationException(
                        "Invalid '" + ConstructedAttributeNodeName + 
                        "' node: one of the attribute node's arguments could not be evaluated at compile-time.");
                }

                int paramCount = attrCtor.Parameters.Count();
                if (args.Length != paramCount)
                {
                    throw new InvalidOperationException(
                        "Invalid '" + ConstructedAttributeNodeName + "' node: '" +
                        attrCtor.FullName + "' takes " + paramCount + (paramCount == 1 ? " argument" : " arguments") + ", but is given " +
                        args.Length + ".");
                }
                return new ConstructedAttribute(attrCtor, args);
            });
        }

        /// <summary>
        /// Parses an intrinsic attribute: an named, well-known attribute that
        /// takes a sequence of compile-time constant expressions.
        /// </summary>
        /// <param name="State">The parser state.</param>
        /// <param name="Node">The node that contains the attribute.</param>
        /// <returns></returns>
        public static INodeStructure<IAttribute> ParseIntrinsicAttribute(ParserState State, LNode Node)
        {
            if (Node.ArgCount == 0)
            {
                throw new InvalidOperationException(
                    "Invalid '" + IntrinsicAttributeNodeName + "' node: '" +
                    IntrinsicAttributeNodeName +
                    "' nodes must have at least one argument, representing the attribute's name.");
            }

            // Format: #intrinsic(<attribute_name>, <argument_expressions...>)
            return new LazyValueStructure<IAttribute>(Node, () =>
            {
                var attrName = IRParser.GetIdOrString(Node.Args[0]);
                var args = ExpressionParsers.ParseExpressions(State, Node.Args.Skip(1))
                    .Select(item => item.Evaluate())
                    .ToArray();

                if (args.Any(item => item == null))
                {
                    throw new InvalidOperationException(
                        "Invalid '" + IntrinsicAttributeNodeName +
                        "' node: one of the attribute node's arguments could not be evaluated at compile-time.");
                }

                return new IntrinsicAttribute(attrName, args);
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
                    { IntrinsicAttributeNodeName, ParseIntrinsicAttribute },

                    // Not-so-parameterless primitive attributes:
                    { SingletonNodeName, ParseSingletonAttribute },
                    { AssociatedTypeNodeName, ParseAssociatedTypeAttribute },
                    { OperatorNodeName, ParseOperatorAttribute },
                    { DocumentationNodeName, ParseDocumentationAttribute },

                    // Parameterless primitive attributes:
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
                    { IndexerNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.IndexerAttribute) },
                    { InNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.InAttribute) },
                    { OutNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.OutAttribute) },
                    { ImportNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ImportAttribute) },
                    { InitOnlyNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.InitOnlyAttribute) },
                    { TotalInitializationNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.TotalInitializationAttribute) },
                    { InlineNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.InlineAttribute) },
                    { VarArgsNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.VarArgsAttribute) },

                    // Inheritance attributes
                    { AbstractNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.AbstractAttribute) },
                    { VirtualNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.VirtualAttribute) },

                    // Type attributes
                    { StaticTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.StaticTypeAttribute) },
                    { ReferenceTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ReferenceTypeAttribute) },
                    { ValueTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.ValueTypeAttribute) },
                    { InterfaceTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.InterfaceAttribute) },
                    { EnumTypeNodeName, CreateConstantAttributeParser(PrimitiveAttributes.Instance.EnumAttribute) }
                });
            }
        }

        #endregion
    }
}
