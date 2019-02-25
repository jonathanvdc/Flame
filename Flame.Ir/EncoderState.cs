using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Les;

namespace Flame.Ir
{
    /// <summary>
    /// Encodes Flame's intermediate representation as Loyc LNodes.
    /// </summary>
    public sealed class EncoderState
    {
        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        /// <param name="codec">The codec to use for encoding.</param>
        /// <param name="factory">The node factory to use for creating nodes.</param>
        public EncoderState(IrCodec codec, LNodeFactory factory)
        {
            this.Codec = codec;
            this.Factory = factory;
        }

        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        /// <param name="codec">The codec to use for encoding.</param>
        public EncoderState(IrCodec codec)
            : this(
                codec,
                new LNodeFactory(EmptySourceFile.Default))
        { }

        /// <summary>
        /// Instantiates a Flame IR encoder.
        /// </summary>
        public EncoderState()
            : this(IrCodec.Default)
        { }

        /// <summary>
        /// Gets the codec used by this encoder.
        /// </summary>
        /// <returns>A Flame IR codec.</returns>
        public IrCodec Codec { get; private set; }

        /// <summary>
        /// Gets the node factory for this encoder.
        /// </summary>
        /// <returns>A node factory.</returns>
        public LNodeFactory Factory { get; private set; }

        /// <summary>
        /// Creates an encoder state that uses a particular codec
        /// but retains all other fields.
        /// </summary>
        /// <param name="newCodec">The new codec to use.</param>
        /// <returns>An encoder state that uses <paramref name="newCodec"/>.</returns>
        public EncoderState WithCodec(IrCodec newCodec)
        {
            return new EncoderState(newCodec, Factory);
        }

        internal static readonly Symbol typeHintSymbol = GSymbol.Get("#type");
        internal static readonly Symbol methodHintSymbol = GSymbol.Get("#method");
        internal static readonly Symbol parameterSymbol = GSymbol.Get("#param");
        internal static readonly Symbol entryPointBlockSymbol = GSymbol.Get("#entry_point");
        internal static readonly Symbol basicBlockSymbol = GSymbol.Get("#block");
        internal static readonly Symbol tryFlowExceptionSymbol = GSymbol.Get("#exception");
        internal static readonly Symbol unreachableFlowSymbol = GSymbol.Get("#unreachable");

        /// <summary>
        /// Encodes a type reference.
        /// </summary>
        /// <param name="type">The type reference to encode.</param>
        /// <returns>
        /// An encoded type reference.
        /// </returns>
        public LNode Encode(IType type)
        {
            return Codec.Types.Encode(type, this);
        }

        /// <summary>
        /// Encodes a method reference.
        /// </summary>
        /// <param name="method">The method reference to encode.</param>
        /// <returns>
        /// An encoded method reference.
        /// </returns>
        public LNode Encode(IMethod method)
        {
            return Codec.TypeMembers.Encode(method, this);
        }

        /// <summary>
        /// Encodes a field reference.
        /// </summary>
        /// <param name="field">The field reference to encode.</param>
        /// <returns>
        /// An encoded field reference.
        /// </returns>
        public LNode Encode(IField field)
        {
            return Codec.TypeMembers.Encode(field, this);
        }

        /// <summary>
        /// Encodes a property reference.
        /// </summary>
        /// <param name="property">The property reference to encode.</param>
        /// <returns>
        /// An encoded property reference.
        /// </returns>
        public LNode Encode(IProperty property)
        {
            return Codec.TypeMembers.Encode(property, this);
        }

        public LNode Encode(IGenericMember genericMember)
        {
            if (genericMember is IType)
            {
                return Factory.Call(typeHintSymbol, Encode((IType)genericMember));
            }
            else
            {
                return Factory.Call(methodHintSymbol, Encode((IMethod)genericMember));
            }
        }

        /// <summary>
        /// Encodes an instruction prototype.
        /// </summary>
        /// <param name="prototype">The instruction prototype to encode.</param>
        /// <returns>
        /// An encoded instruction prototype.
        /// </returns>
        public LNode Encode(InstructionPrototype prototype)
        {
            return Codec.Instructions.Encode(prototype, this);
        }

        /// <summary>
        /// Encodes a constant value.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>An encoded constant value.</returns>
        public LNode Encode(Constant value)
        {
            return Codec.Constants.Encode(value, this);
        }

        /// <summary>
        /// Encodes a method lookup strategy as an LNode.
        /// </summary>
        /// <param name="lookup">A method lookup strategy.</param>
        /// <returns>
        /// An LNode that represents <paramref name="lookup"/>.
        /// </returns>
        public LNode Encode(MethodLookup lookup)
        {
            switch (lookup)
            {
                case MethodLookup.Static:
                    return Factory.Id("static");
                case MethodLookup.Virtual:
                    return Factory.Id("virtual");
                default:
                    throw new NotSupportedException(
                        "Cannot encode unknown method lookup type '" + lookup.ToString() + "'.");
            }
        }

        /// <summary>
        /// Encodes a Boolean constant.
        /// </summary>
        /// <param name="value">A Boolean constant to encode.</param>
        /// <returns>
        /// The encoded Boolean constant.
        /// </returns>
        public LNode Encode(bool value)
        {
            return Encode(BooleanConstant.Create(value));
        }

        /// <summary>
        /// Encodes an unqualified name as a simple name.
        /// </summary>
        /// <param name="name">
        /// An unqualified name to encode. Simple names are encoded
        /// as such. Other names are encoded as simple names by taking
        /// their string representation.
        /// </param>
        /// <returns>An encoded node.</returns>
        public LNode Encode(UnqualifiedName name)
        {
            if (name is SimpleName)
            {
                var simple = (SimpleName)name;
                var simpleNameNode = Factory.Id(simple.Name);
                if (simple.TypeParameterCount == 0)
                {
                    return simpleNameNode;
                }
                else
                {
                    return Factory.Call(
                        simpleNameNode,
                        Factory.Literal(simple.TypeParameterCount));
                }
            }
            else
            {
                return Factory.Id(name.ToString());
            }
        }

        /// <summary>
        /// Encodes a qualified name as a sequence of simple names.
        /// </summary>
        /// <param name="name">
        /// A qualified name to encode. Simple names in the qualified
        /// name are encoded as such. Other names are encoded as simple
        /// names by taking their string representation.
        /// </param>
        /// <returns>An encoded node.</returns>
        public LNode Encode(QualifiedName name)
        {
            var accumulator = Encode(name.Qualifier);
            for (int i = 1; i < name.PathLength; i++)
            {
                accumulator = Factory.Call(
                    CodeSymbols.ColonColon,
                    accumulator,
                    Encode(name.Path[i]));
            }
            return accumulator;
        }

        /// <summary>
        /// Encodes an attribute as an LNode.
        /// </summary>
        /// <param name="attribute">
        /// The attribute to encode.
        /// </param>
        /// <returns>An encoded node.</returns>
        public LNode Encode(IAttribute attribute)
        {
            return Codec.Attributes.Encode(attribute, this);
        }

        /// <summary>
        /// Encodes a parameter definition as an LNode.
        /// </summary>
        /// <param name="parameter">The parameter to encode.</param>
        /// <returns>An encoded node.</returns>
        public LNode EncodeDefinition(Parameter parameter)
        {
            if (parameter.Name.ToString() == "")
            {
                return Encode(parameter.Type)
                    .WithAttrs(new VList<LNode>(Encode(parameter.Attributes)));
            }
            else
            {
                return Factory.Call(
                    parameterSymbol,
                    Encode(parameter.Type),
                    Encode(parameter.Name))
                    .WithAttrs(new VList<LNode>(Encode(parameter.Attributes)));
            }
        }

        /// <summary>
        /// Encodes an attribute map as a sequence of LNodes.
        /// </summary>
        /// <param name="attributes">
        /// The attribute map to encode.
        /// </param>
        /// <returns>A list of attribute nodes.</returns>
        public IEnumerable<LNode> Encode(AttributeMap attributes)
        {
            return attributes.GetAll().Select(Encode).ToArray();
        }

        /// <summary>
        /// Encodes a type definition.
        /// </summary>
        /// <param name="typeDefinition">
        /// The type definition to encode.
        /// </param>
        /// <returns>
        /// An LNode that represents the type definition.
        /// </returns>
        public LNode EncodeDefinition(IType typeDefinition)
        {
            return Codec.TypeDefinitions.Encode(typeDefinition, this);
        }

        /// <summary>
        /// Encodes a type member definition.
        /// </summary>
        /// <param name="memberDefinition">
        /// The type member definition to encode.
        /// </param>
        /// <returns>
        /// An LNode that represents the type member definition.
        /// </returns>
        public LNode EncodeDefinition(ITypeMember memberDefinition)
        {
            return Codec.TypeMemberDefinitions.Encode(memberDefinition, this);
        }

        /// <summary>
        /// Encodes an assembly definition.
        /// </summary>
        /// <param name="assembly">
        /// The assembly definition to encode.
        /// </param>
        /// <returns>
        /// An LNode that represents the assembly definition.
        /// </returns>
        public LNode EncodeDefinition(IAssembly assembly)
        {
            return IrAssembly.Encode(assembly, this);
        }

        /// <summary>
        /// Encodes a control-flow graph.
        /// </summary>
        /// <param name="graph">The control-flow graph to encode.</param>
        /// <returns>An LNode that represents the control-flow graph.</returns>
        public LNode Encode(FlowGraph graph)
        {
            var blockNameSet = new UniqueNameSet<UniqueTag>(
                tag => tag.Name, "block_");
            var valueNameSet = new UniqueNameSet<UniqueTag>(
                tag => tag.Name, "val_", blockNameSet);

            // Reserve special names.
            valueNameSet.ReserveName(CodeSymbols.Result.Name);
            valueNameSet.ReserveName(tryFlowExceptionSymbol.Name);

            var blockNameMap = new UniqueNameMap<UniqueTag>(blockNameSet);
            var valueNameMap = new UniqueNameMap<UniqueTag>(valueNameSet);

            var blockNodes = new List<LNode>();
            foreach (var block in graph.BasicBlocks)
            {
                var paramNodes = block.Parameters
                    .EagerSelect(param =>
                        Factory.Call(
                            parameterSymbol,
                            Encode(param.Type),
                            EncodeUniqueTag(param.Tag, valueNameMap)));

                var instrNodes = block.Instructions
                    .Select(instr =>
                        Factory.Call(
                            CodeSymbols.Assign,
                            EncodeUniqueTag(instr.Tag, valueNameMap),
                            Encode(instr.Instruction, valueNameMap)))
                    .ToArray();

                blockNodes.Add(
                    Factory.Call(
                        block.Tag == graph.EntryPointTag
                            ? entryPointBlockSymbol
                            : basicBlockSymbol,
                        EncodeUniqueTag(block.Tag, blockNameMap),
                        Factory.Call(CodeSymbols.AltList, paramNodes),
                        Factory.Call(CodeSymbols.Braces, instrNodes),
                        Encode(block.Flow, blockNameMap, valueNameMap)));
            }
            return Factory.Braces(blockNodes);
        }

        private LNode Encode(
            BlockFlow flow,
            UniqueNameMap<UniqueTag> blockNameMap,
            UniqueNameMap<UniqueTag> valueNameMap)
        {
            if (flow is UnreachableFlow)
            {
                return Factory.Id(unreachableFlowSymbol);
            }
            else if (flow is JumpFlow)
            {
                return Factory.Call(
                    CodeSymbols.Goto,
                    Encode(((JumpFlow)flow).Branch, blockNameMap, valueNameMap));
            }
            else if (flow is ReturnFlow)
            {
                return Factory.Call(
                    CodeSymbols.Return,
                    Encode(((ReturnFlow)flow).ReturnValue, valueNameMap));
            }
            else if (flow is TryFlow)
            {
                var tryFlow = (TryFlow)flow;

                return Factory.Call(
                    CodeSymbols.Try,
                    Encode(tryFlow.Instruction, valueNameMap),
                    Encode(tryFlow.SuccessBranch, blockNameMap, valueNameMap),
                    Encode(tryFlow.ExceptionBranch, blockNameMap, valueNameMap));
            }
            else if (flow is SwitchFlow)
            {
                var switchFlow = (SwitchFlow)flow;

                var caseNodes = new List<LNode>();
                foreach (var switchCase in switchFlow.Cases)
                {
                    caseNodes.Add(
                        Factory.Call(
                            CodeSymbols.Case,
                            Factory.Call(CodeSymbols.AltList, switchCase.Values.Select(Encode)),
                            Encode(switchCase.Branch, blockNameMap, valueNameMap)));
                }

                return Factory.Call(
                    CodeSymbols.Switch,
                    Encode(switchFlow.SwitchValue, valueNameMap),
                    Encode(switchFlow.DefaultBranch, blockNameMap, valueNameMap),
                    Factory.Call(CodeSymbols.Braces, caseNodes));
            }
            else
            {
                throw new NotSupportedException(
                    "Cannot encode unknown block flow '" + flow + "'.");
            }
        }

        private LNode Encode(
            Instruction instruction,
            UniqueNameMap<UniqueTag> valueNameMap)
        {
            return Factory.Call(
                Encode(instruction.Prototype),
                instruction.Arguments.Select(
                    tag => EncodeUniqueTag(tag, valueNameMap)));
        }

        private LNode Encode(
            Branch branch,
            UniqueNameMap<UniqueTag> blockNameMap,
            UniqueNameMap<UniqueTag> valueNameMap)
        {
            var argNodes = new List<LNode>();
            foreach (var arg in branch.Arguments)
            {
                switch (arg.Kind)
                {
                    case BranchArgumentKind.TryException:
                        argNodes.Add(Factory.Id(tryFlowExceptionSymbol));
                        break;
                    case BranchArgumentKind.TryResult:
                        argNodes.Add(Factory.Id(CodeSymbols.Result));
                        break;
                    default:
                        argNodes.Add(EncodeUniqueTag(arg.ValueOrNull, valueNameMap));
                        break;
                }
            }

            return Factory.Call(EncodeUniqueTag(branch.Target, blockNameMap), argNodes);
        }

        private LNode EncodeUniqueTag(UniqueTag tag, UniqueNameMap<UniqueTag> nameMap)
        {
            return Factory.Id(nameMap[tag]);
        }
    }
}
