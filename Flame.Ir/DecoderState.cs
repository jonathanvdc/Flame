using System;
using System.Collections.Generic;
using System.Numerics;
using Flame.Compiler;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// Decodes Loyc LNodes to Flame's intermediate representation.
    /// </summary>
    public sealed class DecoderState
    {
        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="typeResolver">A read-only type resolver for resolving types.</param>
        /// <param name="codec">A Flame IR codec.</param>
        /// <param name="scope">The decoder's scope.</param>
        private DecoderState(
            ILog log,
            ReadOnlyTypeResolver typeResolver,
            IrCodec codec,
            TypeParent scope)
        {
            this.Log = log;
            this.TypeResolver = typeResolver;
            this.Codec = codec;
            this.Scope = scope;
        }

        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="typeResolver">A read-only type resolver for resolving types.</param>
        /// <param name="codec">A Flame IR codec.</param>
        public DecoderState(
            ILog log,
            ReadOnlyTypeResolver typeResolver,
            IrCodec codec)
            : this(log, typeResolver, codec, TypeParent.Nothing)
        { }

        /// <summary>
        /// Creates a decoder that relies on the default codec.
        /// </summary>
        /// <param name="log">A log to use for error and warning messages.</param>
        /// <param name="typeResolver">A read-only type resolver for resolving types.</param>
        public DecoderState(ILog log, ReadOnlyTypeResolver typeResolver)
            : this(log, typeResolver, IrCodec.Default)
        { }

        /// <summary>
        /// Gets a log to use for error and warning messages.
        /// </summary>
        /// <returns>A log.</returns>
        public ILog Log { get; private set; }

        /// <summary>
        /// Gets the codec used by this decoder.
        /// </summary>
        /// <returns>The codec.</returns>
        public IrCodec Codec { get; private set; }

        /// <summary>
        /// Gets the read-only type resolver for this decoder state.
        /// </summary>
        /// <returns>A type resolver.</returns>
        public ReadOnlyTypeResolver TypeResolver { get; private set; }

        /// <summary>
        /// Gets the scope in which elements are decoded.
        /// </summary>
        /// <returns>
        /// The scope in which elements are decoded, represented as a type parent.
        /// </returns>
        public TypeParent Scope { get; private set; }

        /// <summary>
        /// Gets the type that either is or defines the current
        /// decoding scope.
        /// </summary>
        public IType DefiningType =>
            Scope.IsType ? Scope.Type : Scope.Method.ParentType;

        /// <summary>
        /// Creates a new decoder state that is identical to this
        /// decoder state in every way except for the decoding scope.
        /// </summary>
        /// <param name="newScope">
        /// The decoding scope for the new decoder state.
        /// </param>
        /// <returns>
        /// A new decoder state.
        /// </returns>
        public DecoderState WithScope(TypeParent newScope)
        {
            return new DecoderState(Log, TypeResolver, Codec, newScope);
        }

        /// <summary>
        /// Decodes an LNode as a type reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded type reference.
        /// </returns>
        public IType DecodeType(LNode node)
        {
            return Codec.Types.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as a method reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded method reference.
        /// </returns>
        public IMethod DecodeMethod(LNode node)
        {
            return Codec.Methods.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as a type definition.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded type definition.</returns>
        public IType DecodeTypeDefinition(LNode node)
        {
            return Codec.TypeDefinitions.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as a generic parameter definition.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded generic parameter.</returns>
        public IGenericParameter DecodeGenericParameterDefinition(LNode node)
        {
            return (IGenericParameter)DecodeTypeDefinition(node);
        }

        /// <summary>
        /// Decodes an LNode as a type member definition.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded type member definition.</returns>
        public ITypeMember DecodeTypeMemberDefinition(LNode node)
        {
            return Codec.TypeMemberDefinitions.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as an instruction prototype.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded instruction prototype.
        /// </returns>
        public InstructionPrototype DecodeInstructionProtoype(LNode node)
        {
            return Codec.Instructions.Decode(node, this);
        }

        private static readonly Dictionary<Symbol, MethodLookup> methodLookupDecodeMap =
            new Dictionary<Symbol, MethodLookup>()
        {
            { GSymbol.Get("static"), MethodLookup.Static },
            { GSymbol.Get("virtual"), MethodLookup.Virtual }
        };

        /// <summary>
        /// Decodes an LNode as a method lookup strategy.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A method lookup strategy.</returns>
        public MethodLookup DecodeMethodLookup(LNode node)
        {
            MethodLookup result;
            if (AssertDecodeEnum(node, methodLookupDecodeMap, "method lookup strategy", out result))
            {
                return result;
            }
            else
            {
                return MethodLookup.Static;
            }
        }

        /// <summary>
        /// Decodes an LNode as a Boolean constant.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A Boolean constant.</returns>
        public bool DecodeBoolean(LNode node)
        {
            var literal = DecodeConstant(node);
            if (literal == null)
            {
                // Couldn't decode the node, but that's been logged
                // already.
                return false;
            }
            else if (literal is BooleanConstant)
            {
                // Node parsed successfully as a Boolean literal.
                return ((BooleanConstant)literal).Value;
            }
            else
            {
                Log.LogSyntaxError(
                    node,
                    new Text("expected a Boolean literal."));
                return false;
            }
        }

        /// <summary>
        /// Decodes an LNode as a 32-bit signed integer constant.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A 32-bit signed integer.</returns>
        public int DecodeInt32(LNode node)
        {
            int result;
            AssertDecodeInt32(node, out result);
            return result;
        }

        /// <summary>
        /// Decodes an LNode as a 32-bit signed integer constant and
        /// returns a Boolean flag telling if the decoding operation
        /// was successful.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <param name="result">A 32-bit signed integer.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="node"/> was successfully decoded as a
        /// 32-bit signed integer; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeInt32(LNode node, out int result)
        {
            var literal = DecodeConstant(node);
            if (literal == null)
            {
                // Couldn't decode the node, but that's been logged
                // already.
                result = 0;
                return false;
            }
            else if (literal is IntegerConstant)
            {
                // Node parsed successfully as an integer literal.
                // Cast it to an int32.
                result = ((IntegerConstant)literal).ToInt32();
                return true;
            }
            else
            {
                Log.LogSyntaxError(
                    node,
                    new Text("expected a 32-bit signed integer literal."));
                result = 0;
                return false;
            }
        }

        /// <summary>
        /// Decodes an LNode as a constant value.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decoded constant.</returns>
        public Constant DecodeConstant(LNode node)
        {
            return Codec.Constants.Decode(node, this);
        }

        /// <summary>
        /// Decodes an id node using a symbol-to-value mapping.
        /// An error is reported if the node cannot be decoded.
        /// </summary>
        /// <param name="node">A node to decode.</param>
        /// <param name="decodeMap">
        /// A mapping of symbols to values that is used for
        /// decoding the node.
        /// </param>
        /// <param name="enumDescription">
        /// A short description of the type of value that is being
        /// decoded, e.g., "method lookup strategy".
        /// </param>
        /// <param name="result">
        /// The decoded value, if any.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node could be decoded; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeEnum<T>(
            LNode node,
            IReadOnlyDictionary<Symbol, T> decodeMap,
            string enumDescription,
            out T result)
        {
            if (!node.IsId)
            {
                Log.LogSyntaxError(
                    node,
                    FeedbackHelpers.QuoteEven(
                        "expected " + enumDescription + " (",
                        FeedbackHelpers.SpellNodeKind(LNodeKind.Id),
                        " node) but got ",
                        FeedbackHelpers.SpellNodeKind(node),
                        " node."));
                result = default(T);
                return false;
            }

            if (decodeMap.TryGetValue(node.Name, out result))
            {
                return true;
            }
            else
            {
                // Create a sorted list of all admissible values.
                var sortedKeys = new List<string>();
                foreach (var item in decodeMap.Keys)
                {
                    sortedKeys.Add(item.Name);
                }
                sortedKeys.Sort();

                // Generate a lengthy message that details exactly what
                // is admissible.
                var message = new List<MarkupNode>();
                message.Add("unknown " + enumDescription + " ");
                message.Add(node.Name.Name);
                message.Add("; expected ");
                for (int i = 0; i < sortedKeys.Count - 1; i++)
                {
                    message.Add(sortedKeys[i]);
                    if (i < sortedKeys.Count - 2)
                    {
                        message.Add(", ");
                    }
                    else
                    {
                        message.Add(" or ");
                    }
                }
                message.Add(sortedKeys[sortedKeys.Count - 1]);
                message.Add(".");

                Log.LogSyntaxError(
                    node,
                    FeedbackHelpers.QuoteEven(message.ToArray()));

                return false;
            }
        }

        /// <summary>
        /// Decodes an LNode as a simple name. Logs an error if the decoding
        /// process fails.
        /// </summary>
        /// <param name="node">A node to decode as a simple name.</param>
        /// <param name="name">The name described by <paramref name="node"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="node"/> can be decoded as a simple
        /// name; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeSimpleName(LNode node, out SimpleName name)
        {
            if (node.IsId)
            {
                name = new SimpleName(node.Name.Name);
                return true;
            }
            else if (node.IsCall)
            {
                var nameNode = node.Target;
                int arity;
                if (!FeedbackHelpers.AssertIsId(nameNode, Log)
                    || !FeedbackHelpers.AssertArgCount(node, 1, Log)
                    || !AssertDecodeInt32(node.Args[0], out arity))
                {
                    name = null;
                    return false;
                }

                name = new SimpleName(nameNode.Name.Name, arity);
                return true;
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    Log,
                    node,
                    FeedbackHelpers.QuoteEven(
                        "expected a simple name, which can either be a simple id (e.g., ",
                        "Name",
                        ") or a call to an id that specifies the number of generic parameters (e.g., ",
                        "Name(2)",
                        ")."));
                name = null;
                return false;
            }
        }

        /// <summary>
        /// Decodes an LNode as a simple name. Logs an error if the decoding
        /// process fails.
        /// </summary>
        /// <param name="node">A node to decode as a simple name.</param>
        /// <param name="name">The name described by <paramref name="node"/>.</param>
        /// <returns>
        /// The name described by <paramref name="node"/> if <paramref name="node"/> can
        /// be decoded as a simple name; otherwise, a sensible default simple name.
        /// </returns>
        public SimpleName DecodeSimpleName(LNode node)
        {
            SimpleName result;
            if (AssertDecodeSimpleName(node, out result))
            {
                return result;
            }
            else
            {
                // Use the empty string. That's probably the best we can
                // do. We could have used something like `<error>` but
                // that might confuse, e.g., the "did you mean" functionality
                // in language front-ends. We don't want a compiler to
                // accidentally suggest "did you mean '<error>'?" because
                // that's a terrible error message.
                return new SimpleName("");
            }
        }

        /// <summary>
        /// Decodes an LNode as a qualified name. Logs an error if the decoding
        /// process fails.
        /// </summary>
        /// <param name="node">A node to decode as a qualified name.</param>
        /// <param name="name">The name described by <paramref name="node"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="node"/> can be decoded as a
        /// qualified name; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeQualifiedName(
            LNode node,
            out QualifiedName name)
        {
            if (node.Calls(CodeSymbols.ColonColon))
            {
                QualifiedName prefix;
                SimpleName suffix;
                if (FeedbackHelpers.AssertArgCount(node, 2, Log)
                    && AssertDecodeQualifiedName(node.Args[0], out prefix)
                    && AssertDecodeSimpleName(node.Args[1], out suffix))
                {
                    name = suffix.Qualify(prefix);
                    return true;
                }
                else
                {
                    name = default(QualifiedName);
                    return false;
                }
            }
            else
            {
                SimpleName simple;
                if (AssertDecodeSimpleName(node, out simple))
                {
                    name = simple.Qualify();
                    return true;
                }
                else
                {
                    name = default(QualifiedName);
                    return false;
                }
            }
        }

        /// <summary>
        /// Decodes an LNode as a qualified name. Logs an error if the decoding
        /// process fails.
        /// </summary>
        /// <param name="node">A node to decode as a qualified name.</param>
        /// <param name="name">The name described by <paramref name="node"/>.</param>
        /// <returns>
        /// The name described by <paramref name="node"/> if <paramref name="node"/> can
        /// be decoded as a qualified name; otherwise, a default qualified name.
        /// </returns>
        public QualifiedName DecodeQualifiedName(LNode node)
        {
            QualifiedName result;
            if (AssertDecodeQualifiedName(node, out result))
            {
                return result;
            }
            else
            {
                // Use the empty qualified name. That's probably the best
                // we can do. We could have used something like `<error>` but
                // that might confuse, e.g., the "did you mean" functionality
                // in language front-ends. We don't want a compiler to
                // accidentally suggest "did you mean '<error>'?" because
                // that's a terrible error message.
                return new SimpleName("").Qualify();
            }
        }

        /// <summary>
        /// Decodes an LNode as a reference to a generic member.
        /// Logs an error if the decoding process fails.
        /// </summary>
        /// <param name="node">A node to decode as a generic member.</param>
        /// <param name="name">The name described by <paramref name="node"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="node"/> can be decoded as a
        /// generic member; otherwise, <c>false</c>.
        /// </returns>
        public bool AssertDecodeGenericMember(
            LNode node,
            out IGenericMember genericMember)
        {
            if (node.Calls(EncoderState.typeHintSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(node, 1, Log))
                {
                    genericMember = null;
                    return false;
                }
                else
                {
                    var type = DecodeType(node.Args[0]);
                    genericMember = type;
                    return !(type == null || type is ErrorType);
                }
            }
            else if (node.Calls(EncoderState.methodHintSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(node, 1, Log))
                {
                    genericMember = null;
                    return false;
                }
                else
                {
                    var method = DecodeMethod(node.Args[0]);
                    genericMember = method;
                    return method != null;
                }
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    Log,
                    node,
                    FeedbackHelpers.QuoteEven(
                        "unknown kind of generic member; " +
                        "generic member kinds must be hinted using either ",
                        EncoderState.methodHintSymbol.ToString(),
                        " or ",
                        EncoderState.typeHintSymbol.ToString(),
                        " nodes."));
                genericMember = null;
                return false;
            }
        }

        /// <summary>
        /// Decodes an attribute node.
        /// </summary>
        /// <param name="node">The attribute node to decode.</param>
        /// <returns>An attribute node.</returns>
        public IAttribute DecodeAttribute(LNode node)
        {
            return Codec.Attributes.Decode(node, this);
        }

        /// <summary>
        /// Decodes a sequence of attribute nodes as an attribute map.
        /// </summary>
        /// <param name="attributeNodes">The nodes to decode.</param>
        /// <returns>An attribute map.</returns>
        public AttributeMap DecodeAttributeMap(IEnumerable<LNode> attributeNodes)
        {
            var result = new AttributeMapBuilder();
            foreach (var item in attributeNodes)
            {
                if (item.IsTrivia)
                {
                    continue;
                }

                var attr = DecodeAttribute(item);
                if (attr != null)
                {
                    result.Add(attr);
                }
            }
            return new AttributeMap(result);
        }

        /// <summary>
        /// Decodes a parameter node.
        /// </summary>
        /// <param name="node">A parameter node to decode.</param>
        /// <returns>A decoded parameter.</returns>
        public Parameter DecodeParameter(LNode node)
        {
            var attrs = DecodeAttributeMap(node.Attrs);
            if (node.Calls(EncoderState.parameterSymbol))
            {
                if (!FeedbackHelpers.AssertArgCount(node, 2, Log))
                {
                    return new Parameter(ErrorType.Instance).WithAttributes(attrs);
                }
                else
                {
                    return new Parameter(
                        DecodeType(node.Args[0]),
                        DecodeSimpleName(node.Args[1]),
                        attrs);
                }
            }
            else
            {
                return new Parameter(DecodeType(node)).WithAttributes(attrs);
            }
        }

        /// <summary>
        /// Decodes an assembly.
        /// </summary>
        /// <param name="node">The assembly to decode.</param>
        /// <returns>A decoded assembly.</returns>
        public IAssembly DecodeAssembly(LNode node)
        {
            return IrAssembly.Decode(node, this);
        }
    }
}
