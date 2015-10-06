using Flame.Build;
using Flame.Compiler;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    // IR file contents:
    // - Dependency set
    // - Type table
    // - Method table
    // - Field table
    // - Assembly

    public class IRParser
    {
        #region Static

        public const string DependencyNodeName = "#external_dependency";
        public const string TypeTableName = "#type_table";
        public const string MethodTableName = "#method_table";
        public const string FieldTableName = "#field_table";

        public const string TypeReferenceName = "#type_reference";
        
        public const string FieldReferenceName = "#field_reference";
        public const string MethodReferenceName = "#method_reference";

        /// <summary>
        /// An identifier for generic instances.
        /// </summary>
        public const string GenericInstanceName = "#of";

        /// <summary>
        /// An identifier for members whose declaring member is a generic instance.
        /// </summary>
        public const string GenericInstanceMemberName = "#of_member";

        public const string TypeTableReferenceName = "#type_table_reference";
        public const string FieldTableReferenceName = "#field_table_reference";
        public const string MethodTableReferenceName = "#method_table_reference";

        public const string LocalGenericParameterReferenceName = "#local_generic_parameter";
        public const string MethodGenericParameterReferenceName = "#method_generic_parameter";
        public const string TypeGenericParmaterReferenceName = "#type_generic_parameter";

        public const string NestedTypeName = "#nested_type";
        public const string ArrayTypeName = "#array_type";
        public const string PointerTypeName = "#pointer_type";
        public const string VectorTypeName = "#vector_type";

        public const string RootTypeName = "#root_type";
        public const string IterableTypeName = "#iterable_type";
        public const string IteratorTypeName = "#iterator_type";
        public const string UIntTypeNodeName = "#uint";
        public const string IntTypeNodeName = "#int";
        public const string BitTypeNodeName = "#bit";
        public const string FloatTypeNodeName = "#float";
        public const string CharTypeName = "#char";
        public const string StringTypeName = "#string";
        public const string BooleanTypeName = "#bool";
        public const string VoidTypeName = "#void";

        #region Table/Set Parsing

        /// <summary>
        /// Searches the given sequence of top-level nodes for the given table type,
        /// and returns all entries in the said table nodes, concatenated head to tail.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="TableType"></param>
        /// <returns></returns>
        public static IEnumerable<LNode> GetTableEntries(IEnumerable<LNode> Nodes, string TableType)
        {
            return Nodes.Where(item => item.Name.Name == TableType)
                        .SelectMany(item => item.Args);
        }

        /// <summary>
        /// Parses a table of some kind. Said table is defined
        /// by the concatenation of all nodes of a specific type in the given sequence of nodes.
        /// Each item is parsed by the given parsing function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Nodes">The sequence of nodes to search for the given type of table.</param>
        /// <param name="TableType">The identifier for the type of table to parse.</param>
        /// <param name="ParseEntry">The parsing function for a table entry.</param>
        /// <returns></returns>
        public static IReadOnlyList<T> ParseTable<T>(IEnumerable<LNode> Nodes, string TableType, Func<LNode, T> ParseEntry)
        {
            return GetTableEntries(Nodes, TableType).Select(ParseEntry)
                                                    .ToArray();
        }

        /// <summary>
        /// Parses a set of some kind. Said set is defined
        /// by the concatenation of all nodes of a specific type in the given sequence of nodes.
        /// Each item is parsed by the given parsing function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Nodes">The sequence of nodes to search for the given type of set.</param>
        /// <param name="TableType">The identifier for the type of set to parse.</param>
        /// <param name="ParseEntry">The parsing function for a set item.</param>
        /// <returns></returns>
        public static IEnumerable<T> ParseSet<T>(IEnumerable<LNode> Nodes, string TableType, Func<LNode, T> ParseEntry)
        {
            return GetTableEntries(Nodes, TableType).Select(ParseEntry)
                                                    .Distinct();
        }

        /// <summary>
        /// Parses all dependency tables in the given sequence of nodes.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<string> ParseDependencies(IEnumerable<LNode> Nodes)
        {
            return ParseSet(Nodes, DependencyNodeName, item => item.Name.Name);
        }

        #endregion

        #region Reference Parsing

        #region Types

        public static INodeStructure<IType> ParseTypeTableReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #type_table_reference(index)

            return new LazyNodeStructure<IType>(Node, () => State.Header.TypeTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IType> ParseNestedTypeReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #nested_type(declaring_type, "name")

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]);
                string name = (string)Node.Args[1].Value;
                return ((INamespace)declType).Types.First(item => item.Name == name);
            });
        }

        public static INodeStructure<IType> ParseTypeSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #type_reference("full_name")

            return new LazyNodeStructure<IType>(Node, () => State.Binder.BindType((string)Node.Args.Single().Value));
        }

        public static INodeStructure<IType> ParseMethodGenericParameterReference(ParserState State, LNode Node)
        {
            // Format:
            // 
            // #method_generic_parameter(declaring_method, index)
            return new LazyNodeStructure<IType>(Node, () =>
            {
                var declMethod = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
                int index = Convert.ToInt32(Node.Args[1].Value);
                return declMethod.GenericParameters.ElementAt(index);
            });
        }

        public static INodeStructure<IType> ParseTypeGenericParameterReference(ParserState State, LNode Node)
        {
            // Format:
            // 
            // #type_generic_parameter(declaring_type, index)
            return new LazyNodeStructure<IType>(Node, () =>
            {
                var declMethod = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                int index = Convert.ToInt32(Node.Args[1].Value);
                return declMethod.GenericParameters.ElementAt(index);
            });
        }

        public static INodeStructure<IType> ParseGenericTypeInstance(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of(type_definition, type_arguments)

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var genDef = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var tyArgs = Node.Args.Skip(1).Select(item => State.Parser.TypeReferenceParser.Parse(State, item).Value);
                return genDef.MakeGenericType(tyArgs);
            });
        }

        public static INodeStructure<IType> ParseGenericInstanceType(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of_member(declaring_type, type_definition)

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var declType = (GenericTypeBase)State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var defType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[1]).Value;
                return new GenericInstanceType(defType, declType.Resolver, declType);
            });
        }

        public static INodeStructure<IType> ParseArrayType(ParserState State, LNode Node)
        {
            // Format:
            //
            // #array_type(element_type, dimensions)

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var elemType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                int dims = Convert.ToInt32(Node.Args[1].Value);
                return elemType.MakeArrayType(dims);
            });
        }

        public static INodeStructure<IType> ParseVectorType(ParserState State, LNode Node)
        {
            // Format:
            //
            // #vector_type(element_type, dimensions...)

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var elemType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                int[] dims = Node.Args.Skip(1).Select(item => Convert.ToInt32(item.Value)).ToArray();
                return elemType.MakeVectorType(dims);
            });
        }

        public static INodeStructure<IType> ParsePointerType(ParserState State, LNode Node)
        {
            // Format:
            //
            // #pointer_type(element_type, pointer_kind)

            return new LazyNodeStructure<IType>(Node, () =>
            {
                var elemType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var ptrKind = PointerKind.Register((string)Node.Args[1].Value);
                return elemType.MakePointerType(ptrKind);
            });
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreateIndexedTypeParser(Dictionary<int, IType> Types)
        {
            return new Func<ParserState, LNode, INodeStructure<IType>>((state, node) => new ConstantNodeStructure<IType>(node, Types[Convert.ToInt32(node.Args.Single().Value)]));
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreatePrimitiveTypeParser(params IType[] Types)
        {
            var dict = new Dictionary<int, IType>();
            for (int i = 0; i < Types.Length; i++)
            {
                dict[Types[i].GetPrimitiveSize()] = Types[i];
            }
            return CreateIndexedTypeParser(dict);
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreatePredefinedTypeParser(IType Value)
        {
            return new Func<ParserState, LNode, INodeStructure<IType>>((state, node) => new ConstantNodeStructure<IType>(node, Value));
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreateEnvironmentTypeParser(Func<IEnvironment, IType> TypeFactory)
        {
            return new Func<ParserState, LNode, INodeStructure<IType>>((state, node) =>
            {
                return new LazyNodeStructure<IType>(node, () => TypeFactory(state.Environment));
            });
        }

        #endregion

        #region Fields

        public static INodeStructure<IField> ParseFieldTableReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #field_table_reference(index)

            return new LazyNodeStructure<IField>(Node, () => State.Header.FieldTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IField> ParseFieldSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #field_reference(declaring_type, name, is_static)

            return new LazyNodeStructure<IField>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string fieldName = (string)Node.Args[1].Value;
                bool isStatic = (bool)Node.Args[2].Value;
                return declType.GetField(fieldName, isStatic);
            });
        }

        public static INodeStructure<IField> ParseGenericInstanceField(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of_member(declaring_type, field_definition)

            return new LazyNodeStructure<IField>(Node, () =>
            {
                var declType = (GenericTypeBase)State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var defField = State.Parser.FieldReferenceParser.Parse(State, Node.Args[1]).Value;
                return new GenericInstanceField(defField, declType.Resolver, declType);
            });
        }

        #endregion

        #region Methods

        public static INodeStructure<IMethod> ParseMethodTableReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #method_table_reference(index)

            return new LazyNodeStructure<IMethod>(Node, () => State.Header.MethodTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IMethod> ParseMethodSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #method_reference(declaring_type, name, is_static, { generic_parameters_names... }, return_type, { parameter_types... })

            return new LazyNodeStructure<IMethod>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string methodName = (string)Node.Args[1].Value;
                var descMethod = new DescribedMethod(methodName, declType);
                descMethod.IsStatic = (bool)Node.Args[2].Value;
                foreach (var item in Node.Args[3].Args)
                {
                    descMethod.AddGenericParameter(new DescribedGenericParameter(item.Name.Name, descMethod));
                }
                var genericParser = State.Parser.TypeReferenceParser.AddParser(LocalGenericParameterReferenceName, (state, elem) => new LazyNodeStructure<IType>(elem, () => descMethod.GenericParameters.ElementAt(Convert.ToInt32(elem.Args.Single().Value))));
                descMethod.ReturnType = genericParser.Parse(State, Node.Args[4]).Value;
                foreach (var item in Node.Args[5].Args.Select((x, i) => new KeyValuePair<int, LNode>(i, x)))
                {
                    descMethod.AddParameter(new DescribedParameter("arg" + item.Key, genericParser.Parse(State, item.Value).Value));
                }
                return declType.Methods.GetMethod(descMethod);
            });
        }

        public static INodeStructure<IMethod> ParseGenericMethodInstance(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of(type_definition, type_arguments)

            return new LazyNodeStructure<IMethod>(Node, () =>
            {
                var genDef = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
                var tyArgs = Node.Args.Skip(1).Select(item => State.Parser.TypeReferenceParser.Parse(State, item).Value);
                return genDef.MakeGenericMethod(tyArgs);
            });
        }

        public static INodeStructure<IMethod> ParseGenericInstanceMethod(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of_member(declaring_type, method_definition)

            return new LazyNodeStructure<IMethod>(Node, () =>
            {
                var declType = (GenericTypeBase)State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var defField = State.Parser.MethodReferenceParser.Parse(State, Node.Args[1]).Value;
                return new GenericInstanceMethod(defField, declType.Resolver, declType);
            });
        }

        #endregion

        #endregion

        #region Definition Parsing

        #region Common

        public static IRSignature ParseSignature(ParserState State, LNode Node)
        {
            // Format:
            //
            // #member(name, attributes...)

            string name = Node.Args[0].Name.Name;
            var attrs = Node.Args.Skip(1).Select(item => State.Parser.AttributeParser.Parse(State, item));
            return new IRSignature(name, attrs);
        }

        #endregion

        #region Types

        public static INodeStructure<IGenericParameter> ParseGenericParameterDefinition(ParserState State, LNode Node, IGenericMember DeclaringMember)
        {
            // Format:
            //
            // #generic_parameter(#member(name, attributes...), { generic_parameters... }, { constraints... })

            var signature = ParseSignature(State, Node.Args[0]);
            var result = new IRGenericParameter(DeclaringMember, signature);
            result.GenericParameterNodes = new NodeList<IGenericParameter>(Node.Args[1].Args.Select(item => ParseGenericParameterDefinition(State, item, result)));
            result.ConstraintNodes = new NodeList<IGenericConstraint>(Node.Args[2].Args.Select(item => State.Parser.GenericConstraintParser.Parse(State, item)));
            return result;
        }

        public static INodeStructure<IType> ParseUserTypeDefinition(ParserState State, LNode Node, INamespace DeclaringNamespace)
        {
            // Format:
            //
            // #type_definition(#member(name, attributes...), { generic_parameters... }, { base_types... }, { nested_types... }, { members... })

            var signature = ParseSignature(State, Node.Args[0]);
            var result = new IRTypeDefinition(DeclaringNamespace, signature);
            result.GenericParameterNodes = new NodeList<IGenericParameter>(Node.Args[1].Args.Select(item => ParseGenericParameterDefinition(State, item, result)));
            result.BaseTypeNodes = new NodeList<IType>(Node.Args[2].Args.Select(item => State.Parser.TypeReferenceParser.Parse(State, item)));
            result.NestedTypeNodes = new NodeList<IType>(Node.Args[3].Args.Select(item => State.Parser.TypeDefinitionParser.Parse(State, item, result)));
            result.MemberNodes = new NodeList<ITypeMember>(Node.Args[4].Args.Select(item => State.Parser.TypeMemberParser.Parse(State, item, result)));
            return result;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Parses a field definition.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="DeclaringType"></param>
        /// <returns></returns>
        public static INodeStructure<IField> ParseFieldDefinition(ParserState State, LNode Node, IType DeclaringType)
        {
            if (Node.ArgCount != 3 && Node.ArgCount != 4)
            {
                throw new InvalidOperationException("Invalid '#field' node: '#field' nodes must have exactly three or four arguments.");
            }

            var sig = ParseSignature(State, Node.Args[0]);
            bool isStatic = Convert.ToBoolean(Node.Args[1].Value);
            var fieldType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[2]);
            var result = new IRField(DeclaringType, sig, isStatic, fieldType);
            if (Node.ArgCount == 4)
            {
                result.InitialValueNode = State.Parser.ExpressionParser.Parse(State, Node.Args[3]);
            }
            return result;
        }

        #endregion

        #endregion

        #region Default Parsers

        public static ReferenceParser<IType> DefaultTypeReferenceParser
        {
            get
            {
                return new ReferenceParser<IType>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IType>>>()
                {
                    // References to type definitions
                    { TypeReferenceName, ParseTypeSignatureReference },
                    { TypeTableReferenceName, ParseTypeTableReference },
                    { NestedTypeName, ParseNestedTypeReference },

                    // Environment-dependent types
                    { RootTypeName, CreateEnvironmentTypeParser(env => env.RootType) },
                    { IterableTypeName, CreateEnvironmentTypeParser(env => env.EnumerableType) },
                    { IteratorTypeName, CreateEnvironmentTypeParser(env => env.EnumeratorType) },

                    // Types that have something to do with generics
                    { MethodGenericParameterReferenceName, ParseMethodGenericParameterReference },
                    { TypeGenericParmaterReferenceName, ParseTypeGenericParameterReference },
                    { GenericInstanceName, ParseGenericTypeInstance },
                    { GenericInstanceMemberName, ParseGenericInstanceType },

                    // Container types
                    { ArrayTypeName, ParseArrayType },
                    { PointerTypeName, ParsePointerType },
                    { VectorTypeName, ParseVectorType },

                    // Primitive types
                    { IntTypeNodeName, CreatePrimitiveTypeParser(PrimitiveTypes.Int8, PrimitiveTypes.Int16, PrimitiveTypes.Int32, PrimitiveTypes.Int64) },
                    { UIntTypeNodeName, CreatePrimitiveTypeParser(PrimitiveTypes.UInt8, PrimitiveTypes.UInt16, PrimitiveTypes.UInt32, PrimitiveTypes.UInt64) },
                    { BitTypeNodeName, CreatePrimitiveTypeParser(PrimitiveTypes.Bit8, PrimitiveTypes.Bit16, PrimitiveTypes.Bit32, PrimitiveTypes.Bit64) },
                    { FloatTypeNodeName, CreatePrimitiveTypeParser(PrimitiveTypes.Float32, PrimitiveTypes.Float64) },
                    { BooleanTypeName, CreatePredefinedTypeParser(PrimitiveTypes.Boolean) },
                    { CharTypeName, CreatePredefinedTypeParser(PrimitiveTypes.Char) },
                    { StringTypeName, CreatePredefinedTypeParser(PrimitiveTypes.String) },
                    { VoidTypeName, CreatePredefinedTypeParser(PrimitiveTypes.Void) }
                });
            }
        }

        public static ReferenceParser<IField> DefaultFieldReferenceParser
        {
            get
            {
                return new ReferenceParser<IField>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IField>>>()
                {
                    { FieldReferenceName, ParseFieldSignatureReference },
                    { FieldTableReferenceName, ParseFieldTableReference },
                    { GenericInstanceMemberName, ParseGenericInstanceField }
                });
            }
        }

        public static ReferenceParser<IMethod> DefaultMethodReferenceParser
        {
            get
            {
                return new ReferenceParser<IMethod>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IMethod>>>()
                {
                    { MethodReferenceName, ParseMethodSignatureReference },
                    { MethodTableReferenceName, ParseMethodTableReference },
                    { GenericInstanceName, ParseGenericMethodInstance },
                    { GenericInstanceMemberName, ParseGenericInstanceMethod }
                });
            }
        }

        #endregion

        #endregion

        #region Constructors

        public IRParser(ReferenceParser<IType> TypeReferenceParser, 
                        ReferenceParser<IField> FieldReferenceParser, 
                        ReferenceParser<IMethod> MethodReferenceParser,
                        ReferenceParser<IAttribute> AttributeParser,
                        ReferenceParser<IGenericConstraint> GenericConstraintParser,
                        ReferenceParser<IExpression> ExpressionParser,
                        DefinitionParser<INamespace, IType> TypeDefinitionParser, 
                        DefinitionParser<IType, ITypeMember> TypeMemberParser)
        {
            this.TypeReferenceParser = TypeReferenceParser;
            this.FieldReferenceParser = FieldReferenceParser;
            this.MethodReferenceParser = MethodReferenceParser;
            this.AttributeParser = AttributeParser;
            this.GenericConstraintParser = GenericConstraintParser;
            this.ExpressionParser = ExpressionParser;
            this.TypeDefinitionParser = TypeDefinitionParser;
            this.TypeMemberParser = TypeMemberParser;
        }

        #endregion

        #region Properties

        public ReferenceParser<IType> TypeReferenceParser { get; private set; }
        public ReferenceParser<IField> FieldReferenceParser { get; private set; }
        public ReferenceParser<IMethod> MethodReferenceParser { get; private set; }
        public ReferenceParser<IAttribute> AttributeParser { get; private set; }
        public ReferenceParser<IGenericConstraint> GenericConstraintParser { get; private set; }
        public ReferenceParser<IExpression> ExpressionParser { get; private set; }

        public DefinitionParser<INamespace, IType> TypeDefinitionParser { get; private set; }
        public DefinitionParser<IType, ITypeMember> TypeMemberParser { get; private set; }
        
        #endregion

        #region Methods

        public IRParser WithExpressionParser(ReferenceParser<IExpression> Parser)
        {
            return new IRParser(TypeReferenceParser,
                                FieldReferenceParser,
                                MethodReferenceParser,
                                AttributeParser,
                                GenericConstraintParser,
                                Parser,
                                TypeDefinitionParser,
                                TypeMemberParser);
        }

        #endregion
    }
}
