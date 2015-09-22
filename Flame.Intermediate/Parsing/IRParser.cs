using Flame.Build;
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
        public const string NestedTypeReferenceName = "#nested_type_reference";
        public const string FieldReferenceName = "#field_reference";
        public const string MethodReferenceName = "#method_reference";
        public const string GenericInstanceName = "#of";

        public const string TypeTableReferenceName = "#type_table_reference";
        public const string FieldTableReferenceName = "#field_table_reference";
        public const string MethodTableReferenceName = "#method_table_reference";

        public const string GenericParameterReferenceName = "#generic_parameter_reference";

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
            // #nested_type_reference(declaring_type, "name")

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
            // #type_reference("name")

            return new LazyNodeStructure<IType>(Node, () => State.Binder.BindType((string)Node.Args.Single().Value));
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
                var genericParser = State.Parser.TypeReferenceParser.WithParser(GenericParameterReferenceName, (state, elem) => new LazyNodeStructure<IType>(elem, () => descMethod.GenericParameters.ElementAt(Convert.ToInt32(elem.Args.Single().Value))));
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

        #endregion

        #region Default Parsers

        public static ReferenceParser<IType> DefaultTypeReferenceParser
        {
            get
            {
                return new ReferenceParser<IType>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IType>>>()
                {
                    { TypeReferenceName, ParseTypeSignatureReference },
                    { TypeTableReferenceName, ParseTypeTableReference },
                    { GenericInstanceName, ParseGenericTypeInstance },
                    { NestedTypeReferenceName, ParseNestedTypeReference }
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
                    { FieldTableReferenceName, ParseFieldTableReference }
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
                    { GenericInstanceName, ParseGenericMethodInstance }
                });
            }
        }

        #endregion

        #endregion

        #region Constructors

        public IRParser(ReferenceParser<IType> TypeReferenceParser, ReferenceParser<IField> FieldReferenceParser, 
                        ReferenceParser<IMethod> MethodReferenceParser,
                        DefinitionParser<INamespace, IType> TypeDefinitionParser, DefinitionParser<IType, ITypeMember> TypeMemberParser)
        {
            this.TypeReferenceParser = TypeReferenceParser;
            this.FieldReferenceParser = FieldReferenceParser;
            this.MethodReferenceParser = MethodReferenceParser;
            this.TypeDefinitionParser = TypeDefinitionParser;
            this.TypeMemberParser = TypeMemberParser;
        }

        #endregion

        #region Properties

        public ReferenceParser<IType> TypeReferenceParser { get; private set; }
        public ReferenceParser<IField> FieldReferenceParser { get; private set; }
        public ReferenceParser<IMethod> MethodReferenceParser { get; private set; }

        public DefinitionParser<INamespace, IType> TypeDefinitionParser { get; private set; }
        public DefinitionParser<IType, ITypeMember> TypeMemberParser { get; private set; }
        
        #endregion

        #region Methods

        #endregion
    }
}
