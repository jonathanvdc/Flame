using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
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
            this.typeCache = new ConcurrentDictionary<LNode, IType>();
            this.TypeMemberIndex = new Index<IType, UnqualifiedName, ITypeMember>(
                type =>
                    type.Fields
                    .Concat<ITypeMember>(type.Properties)
                    .Concat<ITypeMember>(type.Methods)
                    .Select(member =>
                        new KeyValuePair<UnqualifiedName, ITypeMember>(
                            member.Name,
                            member)));
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
        /// Gets an index that allows for quick type member lookup.
        /// </summary>
        /// <returns>A type member lookup index.</returns>
        public Index<IType, UnqualifiedName, ITypeMember> TypeMemberIndex { get; private set; }

        /// <summary>
        /// Gets the scope in which elements are decoded.
        /// </summary>
        /// <returns>
        /// The scope in which elements are decoded, represented as a type parent.
        /// </returns>
        public TypeParent Scope { get; private set; }

        private ConcurrentDictionary<LNode, IType> typeCache;

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
            var result = new DecoderState(Log, TypeResolver, Codec, newScope);
            result.typeCache = typeCache;
            return result;
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
            return typeCache.GetOrAdd(node, n => Codec.Types.Decode(n, this));
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
            return (IMethod)Codec.TypeMembers.Decode(node, this);
        }

        /// <summary>
        /// Decoes an LNode as a field reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>A decode field reference.</returns>
        public IField DecodeField(LNode node)
        {
            return (IField)Codec.TypeMembers.Decode(node, this);
        }

        /// <summary>
        /// Decodes an LNode as a property reference.
        /// </summary>
        /// <param name="node">The node to decode.</param>
        /// <returns>
        /// A decoded property reference.
        /// </returns>
        public IProperty DecodeProperty(LNode node)
        {
            return (IProperty)Codec.TypeMembers.Decode(node, this);
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
            else if (literal is IntegerConstant
                && ((IntegerConstant)literal).Spec.Equals(IntegerSpec.UInt1))
            {
                // Node parsed successfully as a Boolean literal.
                return ((IntegerConstant)literal).Value != 0;
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
                if (FeedbackHelpers.AssertArgCount(node, 2, Log))
                {
                    return new Parameter(
                        DecodeType(node.Args[0]),
                        DecodeSimpleName(node.Args[1]),
                        attrs);
                }
                else
                {
                    return new Parameter(ErrorType.Instance).WithAttributes(attrs);
                }
            }
            else
            {
                return new Parameter(DecodeType(node)).WithAttributes(attrs);
            }
        }

        private static BasicBlockBuilder GetBasicBlock(
            Symbol name,
            FlowGraphBuilder graph,
            Dictionary<Symbol, BasicBlockBuilder> blocks)
        {
            BasicBlockBuilder result;
            if (blocks.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                result = graph.AddBasicBlock(name.Name);
                blocks[name] = result;
                return result;
            }
        }

        private static ValueTag GetValueTag(
            Symbol name,
            Dictionary<Symbol, ValueTag> valueTags)
        {
            ValueTag result;
            if (valueTags.TryGetValue(name, out result))
            {
                return result;
            }
            else
            {
                result = new ValueTag(name.Name);
                valueTags[name] = result;
                return result;
            }
        }

        private bool AssertDecodeValueTags(
            IEnumerable<LNode> nodes,
            Dictionary<Symbol, ValueTag> valueTags,
            out IReadOnlyList<ValueTag> tags)
        {
            var results = new List<ValueTag>();
            foreach (var argNode in nodes)
            {
                if (!FeedbackHelpers.AssertIsId(argNode, Log))
                {
                    tags = null;
                    return false;
                }

                results.Add(GetValueTag(argNode.Name, valueTags));
            }
            tags = results;
            return true;
        }

        private bool AssertDecodeBranchArguments(
            IEnumerable<LNode> nodes,
            Dictionary<Symbol, ValueTag> valueTags,
            out IReadOnlyList<BranchArgument> args)
        {
            var results = new List<BranchArgument>();
            foreach (var argNode in nodes)
            {
                if (!FeedbackHelpers.AssertIsId(argNode, Log))
                {
                    args = null;
                    return false;
                }

                var name = argNode.Name;
                if (name == CodeSymbols.Result)
                {
                    results.Add(BranchArgument.TryResult);
                }
                else if (name == EncoderState.tryFlowExceptionSymbol)
                {
                    results.Add(BranchArgument.TryException);
                }
                else
                {
                    results.Add(BranchArgument.FromValue(GetValueTag(argNode.Name, valueTags)));
                }
            }
            args = results;
            return true;
        }

        private bool AssertDecodeBranch(
            LNode node,
            FlowGraphBuilder graph,
            Dictionary<Symbol, BasicBlockBuilder> blocks,
            Dictionary<Symbol, ValueTag> valueTags,
            out Branch result)
        {
            IReadOnlyList<BranchArgument> args;
            if (FeedbackHelpers.AssertIsCall(node, Log)
                && FeedbackHelpers.AssertIsId(node.Target, Log)
                && AssertDecodeBranchArguments(node.Args, valueTags, out args))
            {
                result = new Branch(
                    GetBasicBlock(node.Target.Name, graph, blocks).Tag,
                    args);
                return true;
            }
            else
            {
                result = default(Branch);
                return false;
            }
        }

        private BlockFlow DecodeBlockFlow(
            LNode node,
            FlowGraphBuilder graph,
            Dictionary<Symbol, BasicBlockBuilder> blocks,
            Dictionary<Symbol, ValueTag> valueTags)
        {
            if (node.Calls(CodeSymbols.Goto))
            {
                Branch target;
                if (FeedbackHelpers.AssertArgCount(node, 1, Log)
                    && AssertDecodeBranch(node.Args[0], graph, blocks, valueTags, out target))
                {
                    return new JumpFlow(target);
                }
                else
                {
                    return UnreachableFlow.Instance;
                }
            }
            else if (node.Calls(CodeSymbols.Switch))
            {
                // Decode the value being switched on as well as the default branch.
                Instruction switchVal;
                Branch defaultTarget;
                if (FeedbackHelpers.AssertArgCount(node, 3, Log)
                    && AssertDecodeInstruction(node.Args[0], valueTags, out switchVal)
                    && AssertDecodeBranch(node.Args[1], graph, blocks, valueTags, out defaultTarget))
                {
                    // Decode the switch cases.
                    var switchCases = ImmutableList.CreateBuilder<SwitchCase>();
                    foreach (var caseNode in node.Args[2].Args)
                    {
                        if (!FeedbackHelpers.AssertArgCount(caseNode, 2, Log)
                            || !FeedbackHelpers.AssertIsCall(caseNode.Args[0], Log))
                        {
                            continue;
                        }

                        var constants = ImmutableHashSet.CreateRange<Constant>(
                            caseNode.Args[0].Args
                                .Select(DecodeConstant)
                                .Where(x => x != null));

                        Branch caseTarget;
                        if (AssertDecodeBranch(caseNode.Args[1], graph, blocks, valueTags, out caseTarget))
                        {
                            switchCases.Add(new SwitchCase(constants, caseTarget));
                        }
                    }
                    return new SwitchFlow(switchVal, switchCases.ToImmutable(), defaultTarget);
                }
                else
                {
                    return UnreachableFlow.Instance;
                }
            }
            else if (node.Calls(CodeSymbols.Return))
            {
                Instruction retValue;
                if (FeedbackHelpers.AssertArgCount(node, 1, Log)
                    && AssertDecodeInstruction(node.Args[0], valueTags, out retValue))
                {
                    return new ReturnFlow(retValue);
                }
                else
                {
                    return UnreachableFlow.Instance;
                }
            }
            else if (node.Calls(CodeSymbols.Try))
            {
                Instruction tryValue;
                Branch successBranch;
                Branch exceptionBranch;
                if (FeedbackHelpers.AssertArgCount(node, 3, Log)
                    && AssertDecodeInstruction(node.Args[0], valueTags, out tryValue)
                    && AssertDecodeBranch(node.Args[1], graph, blocks, valueTags, out successBranch)
                    && AssertDecodeBranch(node.Args[2], graph, blocks, valueTags, out exceptionBranch))
                {
                    return new TryFlow(tryValue, successBranch, exceptionBranch);
                }
                else
                {
                    return UnreachableFlow.Instance;
                }
            }
            else if (node.IsIdNamed(EncoderState.unreachableFlowSymbol))
            {
                return UnreachableFlow.Instance;
            }
            else
            {
                FeedbackHelpers.LogSyntaxError(
                    Log,
                    node,
                    Quotation.QuoteEvenInBold(
                        "unknown type of flow; expected one of ",
                        CodeSymbols.Goto.Name, ", ",
                        CodeSymbols.Switch.Name, ", ",
                        CodeSymbols.Try.Name, ", ",
                        CodeSymbols.Return.Name, " or ",
                        EncoderState.unreachableFlowSymbol.Name, "."));
                return UnreachableFlow.Instance;
            }
        }

        private bool AssertDecodeInstruction(
            LNode insnNode,
            Dictionary<Symbol, ValueTag> valueTags,
            out Instruction result)
        {
            if (!FeedbackHelpers.AssertIsCall(insnNode, Log))
            {
                result = default(Instruction);
                return false;
            }

            // Decode the prototype.
            var prototype = DecodeInstructionProtoype(insnNode.Target);

            // Decode the instruction arguments.
            IReadOnlyList<ValueTag> args;
            if (AssertDecodeValueTags(insnNode.Args, valueTags, out args))
            {
                result = prototype.Instantiate(args);
                return true;
            }
            else
            {
                result = default(Instruction);
                return false;
            }
        }

        private BasicBlockBuilder DecodeBasicBlock(
            LNode node,
            FlowGraphBuilder graph,
            Dictionary<Symbol, BasicBlockBuilder> blocks,
            Dictionary<Symbol, ValueTag> valueTags)
        {
            // Each basic block is essentially a
            // (name, parameters, instructions, flow) tuple.
            // We just parse all four elements and call it a day.

            // Parse the block's name and create the block.
            var name = FeedbackHelpers.AssertIsId(node.Args[0], Log)
                ? node.Args[0].Name
                : GSymbol.Empty;

            var blockBuilder = GetBasicBlock(name, graph, blocks);

            // Parse the block's parameter list.
            foreach (var paramNode in node.Args[1].Args)
            {
                if (FeedbackHelpers.AssertArgCount(paramNode, 2, Log))
                {
                    blockBuilder.AppendParameter(
                        new BlockParameter(
                            DecodeType(paramNode.Args[0]),
                            FeedbackHelpers.AssertIsId(paramNode.Args[1], Log)
                                ? GetValueTag(paramNode.Args[1].Name, valueTags)
                                : new ValueTag()));
                }
                else
                {
                    blockBuilder.AppendParameter(new BlockParameter(ErrorType.Instance));
                }
            }

            // Parse the block's instructions.
            foreach (var valueNode in node.Args[2].Args)
            {
                // Decode the instruction.
                Instruction insn;
                if (!FeedbackHelpers.AssertIsCall(valueNode, Log)
                    || !FeedbackHelpers.AssertArgCount(valueNode, 2, Log)
                    || !FeedbackHelpers.AssertIsId(valueNode.Args[0], Log)
                    || !AssertDecodeInstruction(valueNode.Args[1], valueTags, out insn))
                {
                    continue;
                }

                // Append the instruction to the basic block.
                blockBuilder.AppendInstruction(
                    insn,
                    GetValueTag(valueNode.Args[0].Name, valueTags));
            }

            // Parse the block's flow.
            blockBuilder.Flow = DecodeBlockFlow(node.Args[3], graph, blocks, valueTags);

            return blockBuilder;
        }

        /// <summary>
        /// Decodes a control-flow graph as a method body.
        /// </summary>
        /// <param name="node">An encoded control-flow graph.</param>
        /// <returns>
        /// A new method body that includes the decoded control-flow graph.
        /// </returns>
        public FlowGraph DecodeFlowGraph(LNode node)
        {
            // A CFG consists of a list of basic blocks and a specially
            // marked entry point block.

            var graph = new FlowGraphBuilder();

            var blocks = new Dictionary<Symbol, BasicBlockBuilder>();
            var valueTags = new Dictionary<Symbol, ValueTag>();
            var parsedEntryPoint = false;

            // Do a quick pass through all blocks for determinism: we want
            // to define the blocks in the same order as the original IR.
            foreach (var blockNode in node.Args)
            {
                if (!FeedbackHelpers.AssertArgCount(blockNode, 4, Log))
                {
                    // Log the error and return an empty flow graph.
                    return new FlowGraph();
                }

                var name = FeedbackHelpers.AssertIsId(blockNode.Args[0], Log)
                    ? blockNode.Args[0].Name
                    : GSymbol.Empty;

                // Define the basic block for determinism.
                GetBasicBlock(name, graph, blocks);
            }

            foreach (var blockNode in node.Args)
            {
                // Parse the basic block.
                var blockBuilder = DecodeBasicBlock(blockNode, graph, blocks, valueTags);

                // Entry points get special treatment.
                if (blockNode.Calls(EncoderState.entryPointBlockSymbol))
                {
                    if (parsedEntryPoint)
                    {
                        Log.LogSyntaxError(
                            blockNode,
                            "there can be only one entry point block in a control-flow graph.");
                    }

                    parsedEntryPoint = true;

                    // Update the graph's entry point.
                    var oldEntryPointTag = graph.EntryPointTag;
                    graph.EntryPointTag = blockBuilder.Tag;
                    graph.RemoveBasicBlock(oldEntryPointTag);
                }
            }

            if (!parsedEntryPoint)
            {
                Log.LogSyntaxError(
                    node,
                    "all control-flow graphs must define exactly one " +
                    "entry point, but this one doesn't.");
            }

            return graph.ToImmutable();
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
