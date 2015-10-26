using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    // IR file contents:
    // - Runtime library dependency set
    // - Dependency set
    // - Type table
    // - Method table
    // - Field table
    // - Assembly

    public class IRParser
    {
        #region Static

        public const string NullNodeName = "#null";

        public const string RuntimeDependencyNodeName = "#runtime_dependency";
        public const string LibraryDependencyNodeName = "#external_dependency";
        public const string TypeTableName = "#type_table";
        public const string MethodTableName = "#method_table";
        public const string FieldTableName = "#field_table";

        public const string TypeReferenceName = "#type_reference";

        public const string FieldReferenceName = "#field_reference";
        public const string MethodReferenceName = "#method_reference";
        public const string ConstructorReferenceName = "#ctor_reference";
        public const string AccessorReferenceName = "#accessor_reference";

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
        public const string TypeGenericParamaterReferenceName = "#type_generic_parameter";

        public const string NestedTypeName = "#nested_type";
        public const string DelegateTypeName = "#delegate_type";
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

        public const string TypeConstraintName = "#type_constraint";

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
                        .SelectMany(item => item.Args)
                        .SelectMany(NodeFactory.UnpackBlock);
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
        /// Parses a table of some kind. Said table is defined
        /// by the concatenation of all nodes of a specific type in the given sequence of nodes.
        /// Each item is parsed by the given parsing function.
        /// The results of this operation are added to the given output list, which is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Nodes"></param>
        /// <param name="TableType"></param>
        /// <param name="ParseEntry"></param>
        /// <param name="OutputTable"></param>
        /// <returns></returns>
        public static IList<T> ParseTable<T>(IEnumerable<LNode> Nodes, string TableType, Func<LNode, T> ParseEntry, IList<T> OutputTable)
        {
            foreach (var item in GetTableEntries(Nodes, TableType))
            {
                OutputTable.Add(ParseEntry(item));
            }
            return OutputTable;
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
        /// Gets the given node's name if it is an identifier, or
        /// its string value in any other case.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static string GetIdOrString(LNode Node)
        {
            return Node.IsId ? Node.Name.Name : (Node.Value == null ? null : Node.Value.ToString());
        }

        /// <summary>
        /// Parses all dependency tables of the given type in the given sequence of nodes.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<string> ParseDependencies(IEnumerable<LNode> Nodes, string TableType)
        {
            return ParseSet(Nodes, TableType, GetIdOrString);
        }

        /// <summary>
        /// Parses all library dependency tables in the given
        /// sequence of nodes.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<string> ParseLibraryDependencies(IEnumerable<LNode> Nodes)
        {
            return ParseDependencies(Nodes, LibraryDependencyNodeName);
        }

        /// <summary>
        /// Parses all runtime dependency tables in the given
        /// sequence of nodes.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<string> ParseRuntimeDependencies(IEnumerable<LNode> Nodes)
        {
            return ParseDependencies(Nodes, RuntimeDependencyNodeName);
        }

        /// <summary>
        /// Parses the given null node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static INodeStructure<T> ParseNullNode<T>(ParserState State, LNode Node)
            where T : class
        {
            return new ConstantNodeStructure<T>(Node, null);
        }

        #endregion

        #region Reference Parsing

        #region Types

        public static INodeStructure<IType> ParseTypeTableReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #type_table_reference(index)

            return new LazyValueStructure<IType>(Node, () => State.Header.TypeTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IType> ParseNestedTypeReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #nested_type(declaring_type, "name")

            return new LazyValueStructure<IType>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string name = GetIdOrString(Node.Args[1]);
                return ((INamespace)declType).Types.First(item => item.Name == name);
            });
        }

        public static INodeStructure<IType> ParseTypeSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #type_reference("full_name")

            return new LazyValueStructure<IType>(Node, () => State.Binder.BindType(GetIdOrString(Node.Args.Single())));
        }

        public static INodeStructure<IType> ParseMethodGenericParameterReference(ParserState State, LNode Node)
        {
            // Format:
            // 
            // #method_generic_parameter(declaring_method, index)
            return new LazyValueStructure<IType>(Node, () =>
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
            return new LazyValueStructure<IType>(Node, () =>
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

            return new LazyValueStructure<IType>(Node, () =>
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

            return new LazyValueStructure<IType>(Node, () =>
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

            return new LazyValueStructure<IType>(Node, () =>
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

            return new LazyValueStructure<IType>(Node, () =>
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

            return new LazyValueStructure<IType>(Node, () =>
            {
                var elemType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                var ptrKind = PointerKind.Register(GetIdOrString(Node.Args[1]));
                return elemType.MakePointerType(ptrKind);
            });
        }

        public static INodeStructure<IType> ParseDelegateType(ParserState State, LNode Node)
        {
            // Format:
            //
            // #delegate_type(return_type, parameter_types...)

            return new LazyValueStructure<IType>(Node, () =>
            {
                var descMethod = new DescribedMethod("", null);
                descMethod.ReturnType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                int index = 0;
                foreach (var item in Node.Args.Skip(1).Select(item => State.Parser.TypeReferenceParser.Parse(State, item).Value))
                {
                    descMethod.AddParameter(new DescribedParameter("param" + index, item));
                    index++;
                }
                return MethodType.Create(descMethod);
            });
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreateIndexedTypeParser(Dictionary<int, IType> Types)
        {
            return new Func<ParserState, LNode, INodeStructure<IType>>((state, node) =>
            {
                IType resultType;
                int size = Convert.ToInt32(node.Args.Single().Value);
                if (Types.TryGetValue(size, out resultType))
                {
                    return new ConstantNodeStructure<IType>(node, resultType);
                }
                else
                {
                    throw new InvalidOperationException("Could not create a '" + node.Name + "' type of size " + size + ".");
                }
            });
        }

        public static Func<ParserState, LNode, INodeStructure<IType>> CreatePrimitiveTypeParser(params IType[] Types)
        {
            var dict = new Dictionary<int, IType>();
            for (int i = 0; i < Types.Length; i++)
            {
                dict[Types[i].GetPrimitiveSize() * 8] = Types[i];
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
                return new LazyValueStructure<IType>(node, () => TypeFactory(state.Environment));
            });
        }

        #endregion

        #region Fields

        public static INodeStructure<IField> ParseFieldTableReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #field_table_reference(index)

            return new LazyValueStructure<IField>(Node, () => State.Header.FieldTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IField> ParseFieldSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #field_reference(declaring_type, name, is_static)

            return new LazyValueStructure<IField>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string fieldName = GetIdOrString(Node.Args[1]);
                bool isStatic = Convert.ToBoolean(Node.Args[2].Value);
                return declType.GetField(fieldName, isStatic);
            });
        }

        public static INodeStructure<IField> ParseGenericInstanceField(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of_member(declaring_type, field_definition)

            return new LazyValueStructure<IField>(Node, () =>
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

            return new LazyValueStructure<IMethod>(Node, () => State.Header.MethodTable[Convert.ToInt32(Node.Args.Single().Value)].Value);
        }

        public static INodeStructure<IMethod> ParseMethodSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #method_reference(declaring_type, name, is_static, { generic_parameters_names... }, return_type, { parameter_types... })
            //
            // --OR--
            //
            // #ctor_reference(...)

            return new LazyValueStructure<IMethod>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string methodName = GetIdOrString(Node.Args[1]);
                var descMethod = new DescribedMethod(methodName, declType);
                descMethod.IsStatic = Convert.ToBoolean(Node.Args[2].Value);
                descMethod.IsConstructor = Node.Name.Name == ConstructorReferenceName;
                foreach (var item in Node.Args[3].Args)
                {
                    descMethod.AddGenericParameter(new DescribedGenericParameter(GetIdOrString(item), descMethod));
                }
                var genericParser = State.Parser.TypeReferenceParser.WithParser(LocalGenericParameterReferenceName, (state, elem) => new LazyValueStructure<IType>(elem, () => descMethod.GenericParameters.ElementAt(Convert.ToInt32(elem.Args.Single().Value))));
                var genericState = State.WithParser(State.Parser.WithTypeReferenceParser(genericParser));
                descMethod.ReturnType = genericParser.Parse(genericState, Node.Args[4]).Value;
                foreach (var item in Node.Args[5].Args.Select((x, i) => new KeyValuePair<int, LNode>(i, x)))
                {
                    descMethod.AddParameter(new DescribedParameter("arg" + item.Key, genericParser.Parse(genericState, item.Value).Value));
                }
                var result = declType.Methods.GetMethod(descMethod);

                if (result == null)
                {
                    throw new InvalidOperationException("Could not resolve '" + Node + "' as a method or constructor reference.");
                }

                return result;
            });
        }

        public static INodeStructure<IMethod> ParseAccessorSignatureReference(ParserState State, LNode Node)
        {
            // Format:
            //
            // #accessor_reference(declaring_type, property_name, property_is_static, property_type, { indexer_parameter_types... }, accessor_type)

            return new LazyValueStructure<IMethod>(Node, () =>
            {
                var declType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
                string propertyName = GetIdOrString(Node.Args[1]);
                bool propIsStatic = Convert.ToBoolean(Node.Args[2].Value);
                var propType = State.Parser.TypeReferenceParser.Parse(State, Node.Args[3]).Value;
                var propParamTypes = Node.Args[4].Args.Select(item => State.Parser.TypeReferenceParser.Parse(State, item).Value).ToArray();
                var prop = declType.Properties.GetProperty(propertyName, propIsStatic, propType, propParamTypes);
                var accType = AccessorType.Register(GetIdOrString(Node.Args[5]));

                var result = prop.GetAccessor(accType);

                if (result == null)
                {
                    throw new InvalidOperationException("Could not resolve '" + Node + "' as an accessor reference.");
                }

                return result;
            });
        }

        public static INodeStructure<IMethod> ParseGenericMethodInstance(ParserState State, LNode Node)
        {
            // Format:
            //
            // #of(type_definition, type_arguments)

            return new LazyValueStructure<IMethod>(Node, () =>
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

            return new LazyValueStructure<IMethod>(Node, () =>
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

        /// <summary>
        /// Parses the given signature node's name identifier.
        /// </summary>
        /// <param name="SignatureNode"></param>
        /// <returns></returns>
        public static string ParseSignatureName(LNode SignatureNode)
        {
            // Format:
            //
            // #member(name, attributes...)
            //         ^~~~

            return GetIdOrString(SignatureNode.Args[0]);
        }

        /// <summary>
        /// Parses the given signature node within the context
        /// of the specified state.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IRSignature ParseSignature(ParserState State, LNode Node)
        {
            // Format:
            //
            // #member(name, attributes...)

            string name = ParseSignatureName(Node);
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
            result.GenericParameterNodes = ParseGenericParameterList(State, Node.Args[1], result);
            result.ConstraintNodes = new NodeList<IGenericConstraint>(Node.Args[2].Args.Select(item => State.Parser.GenericConstraintParser.Parse(State, item)));
            return result;
        }

        public static INodeStructure<IEnumerable<IGenericParameter>> ParseGenericParameterList(ParserState State, LNode Node, IGenericMember DeclaringMember)
        {
            // Format:
            //
            // { generic_parameters... }

            return new NodeList<IGenericParameter>(Node.Args.Select(item => ParseGenericParameterDefinition(State, item, DeclaringMember)).ToArray());
        }

        public static INodeStructure<IType> ParseUserTypeDefinition(ParserState State, LNode Node, INamespace DeclaringNamespace)
        {
            // Format:
            //
            // #type_definition(#member(name, attributes...), { generic_parameters... }, { base_types... }, { nested_types... }, { members... })

            var signature = ParseSignature(State, Node.Args[0]);
            var result = new IRTypeDefinition(DeclaringNamespace, signature);
            result.GenericParameterNodes = ParseGenericParameterList(State, Node.Args[1], result);
            result.BaseTypeNodes = new NodeList<IType>(Node.Args[2].Args.Select(item => State.Parser.TypeReferenceParser.Parse(State, item)));
            result.NestedTypeNodes = new NodeList<IType>(Node.Args[3].Args.Select(item => State.Parser.TypeDefinitionParser.Parse(State, item, result)));
            result.MemberNodes = new NodeList<ITypeMember>(Node.Args[4].Args.Select(item => State.Parser.TypeMemberParser.Parse(State, item, result)));
            return result;
        }

        #endregion

        #region Namespaces

        /// <summary>
        /// Parses a namespace definition.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="DeclaringNamespace"></param>
        /// <returns></returns>
        public static INodeStructure<INamespaceBranch> ParseNamespace(ParserState State, LNode Node, INamespace DeclaringNamespace)
        {
            var sig = ParseSignature(State, Node.Args[0]);
            var result = new IRNamespace(DeclaringNamespace, sig);

            result.TypeNodes = new NodeList<IType>(Node.Args[1].Args.Select(item => State.Parser.TypeDefinitionParser.Parse(State, item, result)).ToArray());
            result.NamespaceNodes = new NodeList<INamespaceBranch>(Node.Args[2].Args.Select(item => ParseNamespace(State, item, result)).ToArray());

            return result;
        }

        #endregion

        #region Generic Constraints

        /// <summary>
        /// Parses a generic isinstance type constraint.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static INodeStructure<IGenericConstraint> ParseTypeConstraint(ParserState State, LNode Node)
        {
            if (Node.ArgCount != 1)
            {
                throw new InvalidOperationException(
                    "Invalid '" + TypeConstraintName + "' node: " +
                    "'" + TypeConstraintName + "' nodes take exactly one argument.");
            }

            return new LazyValueStructure<IGenericConstraint>(Node, () =>
                new TypeConstraint(State.Parser.TypeReferenceParser.Parse(State, Node.Args.Single()).Value));
        }

        /// <summary>
        /// Creates a "parser" for primitive generic constraints.
        /// </summary>
        /// <param name="Constraint"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IGenericConstraint>> CreateConstantConstraintParser(IGenericConstraint Constraint)
        {
            return new Func<ParserState, LNode, INodeStructure<IGenericConstraint>>((state, node) =>
                new ConstantNodeStructure<IGenericConstraint>(node, Constraint));
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

        #region Parameters

        /// <summary>
        /// Parses a single parameter definition node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static INodeStructure<IParameter> ParseParameter(ParserState State, LNode Node)
        {
            var sig = ParseSignature(State, Node.Args[0]);
            var type = State.Parser.TypeReferenceParser.Parse(State, Node.Args[1]);

            return new IRParameter(sig, type);
        }

        /// <summary>
        /// Parses a list of parameter definition nodes.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static INodeStructure<IEnumerable<IParameter>> ParseParameterList(ParserState State, LNode Node)
        {
            return new NodeList<IParameter>(Node.Args.Select(item => ParseParameter(State, item)).ToArray());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the given method body node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="EnclosingMethod"></param>
        /// <returns></returns>
        public static INodeStructure<IStatement> ParseMethodBody(ParserState State, LNode Node, IMethod EnclosingMethod)
        {
            return new LazyValueStructure<IStatement>(Node, n =>
            {
                var paramList = EnclosingMethod.GetParameters();

                var parserDict = new Dictionary<string, Func<ParserState, LNode, INodeStructure<IExpression>>>()
                { 
                    { ExpressionParsers.GetArgumentNodeName, ExpressionParsers.CreateGetArgumentParser(paramList) },
                    { ExpressionParsers.AddressOfArgumentNodeName, ExpressionParsers.CreateAddressOfArgumentParser(paramList) },
                    { ExpressionParsers.SetArgumentNodeName, ExpressionParsers.CreateSetArgumentParser(paramList) },
                    { ExpressionParsers.ReleaseArgumentNodeName, ExpressionParsers.CreateReleaseArgumentParser(paramList) },
                };

                if (!EnclosingMethod.IsStatic && EnclosingMethod.DeclaringType != null)
                {
                    var thisType = ThisVariable.GetThisType(EnclosingMethod.DeclaringType);

                    parserDict[ExpressionParsers.GetThisNodeName] = ExpressionParsers.CreateGetThisParser(thisType);
                    parserDict[ExpressionParsers.AddressOfThisNodeName] = ExpressionParsers.CreateAddressOfThisParser(thisType);
                    parserDict[ExpressionParsers.SetThisNodeName] = ExpressionParsers.CreateParser(ExpressionParsers.ParseSetThis);
                    // TODO: implement #release_this as an actual release operation, instead of an empty statement.
                    parserDict[ExpressionParsers.ReleaseThisNodeName] = ExpressionParsers.CreateConstantParser(VoidExpression.Instance);
                }

                var exprParser = State.Parser.ExpressionParser.WithParsers(parserDict);
                var newState = State.WithParser(State.Parser.WithExpressionParser(exprParser));

                return ExpressionParsers.ToStatement(ExpressionParsers.ParseExpression(newState, n));
            });
        }

        /// <summary>
        /// Parses a method definition node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="DeclaringType"></param>
        /// <returns></returns>
        public static INodeStructure<IMethod> ParseMethodDefinition(ParserState State, LNode Node, IType DeclaringType)
        {
            // Format:
            //
            // #method(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... })
            //
            // --OR--
            //
            // #method(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... }, body)
            //
            // --OR--
            //
            // #ctor(#member(name, attrs...), { generic_parameters... }, is_static, return_type, { parameters... }, { base_methods... }, body)

            if (Node.ArgCount != 6 && Node.ArgCount != 7)
            {
                throw new InvalidOperationException("Invalid '" + Node.Name.Name + "' node: '" + Node.Name.Name + "' nodes must have exactly six or seven arguments.");
            }

            var sig = ParseSignature(State, Node.Args[0]);
            bool isStatic = Convert.ToBoolean(Node.Args[2].Value);
            bool isCtor = Node.Name.Name == IRMethod.ConstructorNodeName;

            var result = new IRMethod(DeclaringType, sig, isStatic, isCtor);

            var genericParser = State.Parser.TypeReferenceParser.WithParser(LocalGenericParameterReferenceName, (state, elem) => new LazyValueStructure<IType>(elem, () => result.GenericParameters.ElementAt(Convert.ToInt32(elem.Args.Single().Value))));
            var genericState = State.WithParser(State.Parser.WithTypeReferenceParser(genericParser));
            result.GenericParameterNodes = ParseGenericParameterList(genericState, Node.Args[1], result);
            result.ReturnTypeNode = genericParser.Parse(genericState, Node.Args[3]);
            result.ParameterNodes = ParseParameterList(genericState, Node.Args[4]);
            result.BaseMethodNodes = new NodeList<IMethod>(Node.Args[5].Args.Select(item => genericState.Parser.MethodReferenceParser.Parse(genericState, item)).ToArray());

            if (Node.ArgCount > 6)
            {
                result.BodyNode = ParseMethodBody(genericState, Node.Args[6], result);
            }

            return result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Parses the given accessor definition node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="DeclaringProperty"></param>
        /// <returns></returns>
        public static INodeStructure<IAccessor> ParseAccessorDefinition(ParserState State, LNode Node, IProperty DeclaringProperty)
        {
            // Format:
            //
            // #accessor(#member(name, attrs...), accessor_type, is_static, return_type, { parameters... }, { base_methods... })
            //
            // --OR--
            //
            // #accessor(#member(name, attrs...), accessor_type, is_static, return_type, { parameters... }, { base_methods... }, body)

            if (Node.ArgCount != 6 && Node.ArgCount != 7)
            {
                throw new InvalidOperationException("Invalid '" + Node.Name.Name + "' node: '" + Node.Name.Name + "' nodes must have exactly six or seven arguments.");
            }

            var sig = ParseSignature(State, Node.Args[0]);
            var accType = AccessorType.Register(Node.Args[1].Name.Name);
            bool isStatic = Convert.ToBoolean(Node.Args[2].Value);
            var retTypeNode = State.Parser.TypeReferenceParser.Parse(State, Node.Args[3]);

            var result = new IRAccessor(DeclaringProperty, sig, accType, isStatic, retTypeNode);

            result.ParameterNodes = ParseParameterList(State, Node.Args[4]);
            result.BaseMethodNodes = new NodeList<IMethod>(Node.Args[5].Args.Select(item => State.Parser.MethodReferenceParser.Parse(State, item)).ToArray());

            if (Node.ArgCount > 6)
            {
                result.BodyNode = ParseMethodBody(State, Node.Args[6], result);
            }

            return result;
        }

        /// <summary>
        /// Parses the given property definition node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="DeclaringType"></param>
        /// <returns></returns>
        public static INodeStructure<IProperty> ParsePropertyDefinition(ParserState State, LNode Node, IType DeclaringType)
        {
            // Format:
            //
            // #property(#member(name, attrs...), is_static, property_type, { parameters... }, { accessors... })

            if (Node.ArgCount != 5)
            {
                throw new InvalidOperationException("Invalid '" + Node.Name.Name + "' node: '" + Node.Name.Name + "' nodes must have exactly five arguments.");
            }

            var sig = ParseSignature(State, Node.Args[0]);
            bool isStatic = Convert.ToBoolean(Node.Args[1].Value);
            var propTypeNode = State.Parser.TypeReferenceParser.Parse(State, Node.Args[2]);

            var result = new IRProperty(DeclaringType, sig, isStatic, propTypeNode);

            result.IndexerParameterNodes = ParseParameterList(State, Node.Args[3]);
            result.AccessorNodes = new NodeList<IAccessor>(Node.Args[4].Args.Select(item => ParseAccessorDefinition(State, item, result)).ToArray());

            return result;
        }

        #endregion

        #region Assemblies

        /// <summary>
        /// Gets the single '#assembly' node in this set of top-level nodes.
        /// </summary>
        /// <param name="RootNodes"></param>
        /// <returns></returns>
        public static LNode GetAssemblyNode(IEnumerable<LNode> RootNodes)
        {
            return RootNodes.Single(item => item.Name.Name == IRAssembly.AssemblyNodeName);
        }

        /// <summary>
        /// Given the given set of top-level nodes, extracts the 
        /// '#assembly' node's name.
        /// </summary>
        /// <param name="RootNodes"></param>
        /// <returns></returns>
        public static string ParseAssemblyName(IEnumerable<LNode> RootNodes)
        {
            var asmNode = GetAssemblyNode(RootNodes);
            return ParseSignatureName(asmNode.Args[0]);
        }

        #endregion

        #endregion

        #region Default Parsers

        #region References

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
                    { TypeGenericParamaterReferenceName, ParseTypeGenericParameterReference },
                    { GenericInstanceName, ParseGenericTypeInstance },
                    { GenericInstanceMemberName, ParseGenericInstanceType },

                    // Container types
                    { ArrayTypeName, ParseArrayType },
                    { PointerTypeName, ParsePointerType },
                    { VectorTypeName, ParseVectorType },

                    // Delegate types
                    { DelegateTypeName, ParseDelegateType },

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
                    { ConstructorReferenceName, ParseMethodSignatureReference },
                    { AccessorReferenceName, ParseAccessorSignatureReference },
                    { MethodTableReferenceName, ParseMethodTableReference },
                    { GenericInstanceName, ParseGenericMethodInstance },
                    { GenericInstanceMemberName, ParseGenericInstanceMethod }
                });
            }
        }

        public static ReferenceParser<IGenericConstraint> DefaultGenericConstraintParser
        {
            get
            {
                return new ReferenceParser<IGenericConstraint>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IGenericConstraint>>>()
                {
                    { TypeConstraintName, ParseTypeConstraint },

                    { AttributeParsers.ReferenceTypeNodeName, CreateConstantConstraintParser(ReferenceTypeConstraint.Instance) },
                    { AttributeParsers.ValueTypeNodeName, CreateConstantConstraintParser(ValueTypeConstraint.Instance) },
                    { AttributeParsers.EnumTypeNodeName, CreateConstantConstraintParser(EnumConstraint.Instance) }
                });
            }
        }

        #endregion

        #region Definitions

        public static DefinitionParser<INamespace, IType> DefaultTypeDefinitionParser
        {
            get
            {
                return new DefinitionParser<INamespace, IType>(new Dictionary<string, Func<ParserState, LNode, INamespace, INodeStructure<IType>>>()
                {
                    { IRTypeDefinition.TypeDefinitionNodeName, ParseUserTypeDefinition }
                });
            }
        }

        public static DefinitionParser<IType, ITypeMember> DefaultTypeMemberDefinitionParser
        {
            get
            {
                return new DefinitionParser<IType, ITypeMember>(new Dictionary<string, Func<ParserState, LNode, IType, INodeStructure<ITypeMember>>>()
                {
                    { IRField.FieldNodeName, ParseFieldDefinition },
                    { IRMethod.MethodNodeName, ParseMethodDefinition },
                    { IRMethod.ConstructorNodeName, ParseMethodDefinition },
                    { IRProperty.PropertyNodeName, ParsePropertyDefinition }
                });
            }
        }

        #endregion

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

        public IRParser()
            : this(DefaultTypeReferenceParser,
                   DefaultFieldReferenceParser,
                   DefaultMethodReferenceParser,
                   AttributeParsers.DefaultAttributeParser,
                   DefaultGenericConstraintParser,
                   ExpressionParsers.DefaultExpressionParser,
                   DefaultTypeDefinitionParser,
                   DefaultTypeMemberDefinitionParser)
        { }

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

        /// <summary>
        /// Creates a new IR parser whose type reference parser is set to
        /// the given instance.
        /// </summary>
        /// <param name="Parser"></param>
        /// <returns></returns>
        public IRParser WithTypeReferenceParser(ReferenceParser<IType> Parser)
        {
            return new IRParser(Parser,
                                FieldReferenceParser,
                                MethodReferenceParser,
                                AttributeParser,
                                GenericConstraintParser,
                                ExpressionParser,
                                TypeDefinitionParser,
                                TypeMemberParser);
        }

        /// <summary>
        /// Creates a new IR parser whose expression parser is set to
        /// the given instance.
        /// </summary>
        /// <param name="Parser"></param>
        /// <returns></returns>
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

        #region Assembly Parsing

        private static void ParseAssemblyHeader(ParserState State, LNode AssemblyNode, IRAssembly Assembly)
        {
            Assembly.Signature = ParseSignature(State, AssemblyNode.Args[0]);
            Assembly.VersionNode = new VersionNodeStructure(AssemblyNode.Args[1]);
        }

        private static void ParseAssemblyContents(ParserState State, LNode AssemblyNode, IRAssembly Assembly)
        {
            var epParser = State.Parser.MethodReferenceParser.WithParser(NullNodeName, ParseNullNode<IMethod>);

            Assembly.EntryPointNode = epParser.Parse(State, AssemblyNode.Args[2]);
            Assembly.RootNamespace.TypeNodes = new NodeList<IType>(AssemblyNode.Args[3].Args.Select(item => State.Parser.TypeDefinitionParser.Parse(State, item, Assembly.RootNamespace)).ToArray());
            Assembly.RootNamespace.NamespaceNodes = new NodeList<INamespaceBranch>(AssemblyNode.Args[4].Args.Select(item => ParseNamespace(State, item, Assembly.RootNamespace)).ToArray());
        }

        /// <summary>
        /// Parses a single IR assembly from the given external reference resolver and 
        /// set of root nodes.
        /// </summary>
        /// <param name="Resolver"></param>
        /// <param name="RootNodes"></param>
        /// <returns></returns>
        public IRAssembly ParseAssembly(IBinder ExternalBinder, IEnumerable<LNode> RootNodes)
        {
            var tTable = new List<INodeStructure<IType>>();
            var mTable = new List<INodeStructure<IMethod>>();
            var fTable = new List<INodeStructure<IField>>();

            var header = new ImmutableHeader(ExternalBinder, tTable, mTable, fTable);

            var asmNode = GetAssemblyNode(RootNodes);
            var asm = new IRAssembly(IRSignature.Empty, ExternalBinder.Environment);
            var state = new ParserState(this, header, asm);

            // Parse the assembly's header first.
            ParseAssemblyHeader(state, asmNode, asm);

            // Next, parse the type, method and field tables.
            ParseTable(RootNodes, TypeTableName, item => TypeReferenceParser.Parse(state, item), tTable);
            ParseTable(RootNodes, MethodTableName, item => MethodReferenceParser.Parse(state, item), mTable);
            ParseTable(RootNodes, FieldTableName, item => FieldReferenceParser.Parse(state, item), fTable);

            // Now parse the assembly's contents.
            ParseAssemblyContents(state, asmNode, asm);

            return asm;
        }

        #endregion

        #endregion
    }
}
