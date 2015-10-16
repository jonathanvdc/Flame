using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Loyc;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    /// <summary>
    /// Defines a number of functions that parse expressions.
    /// </summary>
    public static class ExpressionParsers
    {
        #region Node names

        public const string ReturnNodeName = "#return";
        public const string ThrowNodeName = "#throw";
        public const string AssertNodeName = "#assert";
        
        public const string SelectNodeName = "#select";
        public const string TaggedNodeName = "#tagged";
        public const string WhileNodeName = "#while";
        public const string DoWhileNodeName = "#do_while";
        public const string ForNodeName = "#for";
        public const string BreakNodeName = "#break";
        public const string ContinueNodeName = "#continue";
        public const string IgnoreNodeName = "#ignore";
        public static readonly string BlockNodeName = CodeSymbols.Braces.Name;

        public const string GetDelegateNodeName = "#get_delegate";
        public const string GetVirtualDelegateNodeName = "#get_virtual_delegate";
        public const string GetExtensionDelegateNodeName = "#get_extension_delegate";
        public const string GetCurriedDelegateNodeName = "#get_curried_delegate";
        public const string InvocationNodeName = "#invoke";

        public const string UnaryNode = "#unary";
        public const string BinaryNode = "#binary";

        public const string DynamicCastNode = "#dynamic_cast";
        public const string StaticCastNode = "#static_cast";
        public const string ReinterpretCastNode = "#reinterpret_cast";
        public const string AsInstanceNode = "#as_instance";
        public const string IsInstanceNode = "#is_instance";

        public const string ConstantInt8Name = "#const_int8";
        public const string ConstantInt16Name = "#const_int16";
        public const string ConstantInt32Name = "#const_int32";
        public const string ConstantInt64Name = "#const_int64";

        public const string ConstantUInt8Name = "#const_uint8";
        public const string ConstantUInt16Name = "#const_uint16";
        public const string ConstantUInt32Name = "#const_uint32";
        public const string ConstantUInt64Name = "#const_uint64";

        public const string ConstantBit8Name = "#const_bit8";
        public const string ConstantBit16Name = "#const_bit16";
        public const string ConstantBit32Name = "#const_bit32";
        public const string ConstantBit64Name = "#const_bit64";

        public const string ConstantFloat32Name = "#const_float32";
        public const string ConstantFloat64Name = "#const_float64";

        public const string ConstantBooleanName = "#const_bool";
        public const string ConstantCharName = "#const_char";
        public const string ConstantStringName = "#const_string";
        public const string ConstantVoidName = "#const_void";
        public const string ConstantNullName = "#const_null";

        /// <summary>
        /// A name for constant nodes that represent
        /// the default value for any given type.
        /// </summary>
        /// <remarks>
        /// Format:
        /// 
        /// #const_default(type)
        /// </remarks>
        public const string ConstantDefaultName = "#const_default";

        /// <summary>
        /// A name for nodes that create new arrays.
        /// </summary>
        /// <remarks>
        /// Format:
        /// 
        /// #new_array(element_type, dimension_expressions...)
        /// </remarks>
        public const string NewArrayName = "#new_array";

        /// <summary>
        /// A name for nodes that create new vectors.
        /// </summary>
        /// <remarks>
        /// Format:
        /// 
        /// #new_vector(element_type, dimension_literals...)
        /// </remarks>
        public const string NewVectorName = "#new_vector";

        #region Variables

        #region Create*VariableName

        /// <summary>
        /// Creates a get-variable node name for the given variable
        /// kind name.
        /// </summary>
        /// <param name="VariableTypeName"></param>
        /// <returns></returns>
        public static string CreateGetVariableName(string VariableKindName)
        {
            return "#get_" + VariableKindName;
        }

        /// <summary>
        /// Creates a set-variable node name for the given variable
        /// kind name.
        /// </summary>
        /// <param name="VariableTypeName"></param>
        /// <returns></returns>
        public static string CreateSetVariableName(string VariableKindName)
        {
            return "#set_" + VariableKindName;
        }

        /// <summary>
        /// Creates a release-variable node name for the given variable
        /// kind name.
        /// </summary>
        /// <param name="VariableTypeName"></param>
        /// <returns></returns>
        public static string CreateReleaseVariableName(string VariableKindName)
        {
            return "#release_" + VariableKindName;
        }

        /// <summary>
        /// Creates an addressof-variable node name for the given variable
        /// kind name.
        /// </summary>
        /// <param name="VariableTypeName"></param>
        /// <returns></returns>
        public static string CreateAddressOfVariableName(string VariableKindName)
        {
            return "#addressof_" + VariableKindName;
        }

        #endregion

        /// <summary>
        /// Gets the "this" variable kind name.
        /// </summary>
        public const string ThisVariableKindName = "this";

        public static readonly string GetThisNodeName = CreateGetVariableName(ThisVariableKindName);
        public static readonly string SetThisNodeName = CreateSetVariableName(ThisVariableKindName);
        public static readonly string ReleaseThisNodeName = CreateReleaseVariableName(ThisVariableKindName);
        public static readonly string AddressOfThisNodeName = CreateAddressOfVariableName(ThisVariableKindName);

        /// <summary>
        /// Gets the argument ("arg") variable kind name.
        /// </summary>
        public const string ArgumentVariableKindName = "arg";

        public static readonly string GetArgumentNodeName = CreateGetVariableName(ArgumentVariableKindName);
        public static readonly string SetArgumentNodeName = CreateSetVariableName(ArgumentVariableKindName);
        public static readonly string ReleaseArgumentNodeName = CreateReleaseVariableName(ArgumentVariableKindName);
        public static readonly string AddressOfArgumentNodeName = CreateAddressOfVariableName(ArgumentVariableKindName);

        /// <summary>
        /// Gets the "local" variable kind name.
        /// </summary>
        public const string LocalVariableKindName = "local";

        public static readonly string GetLocalNodeName = CreateGetVariableName(LocalVariableKindName);
        public static readonly string SetLocalNodeName = CreateSetVariableName(LocalVariableKindName);
        public static readonly string ReleaseLocalNodeName = CreateReleaseVariableName(LocalVariableKindName);
        public static readonly string AddressOfLocalNodeName = CreateAddressOfVariableName(LocalVariableKindName);
        public const string DefineLocalNodeName = "#def_local";

        /// <summary>
        /// Gets the "element" variable kind name.
        /// </summary>
        public const string ElementVariableKindName = "element";

        public static readonly string GetElementNodeName = CreateGetVariableName(ElementVariableKindName);
        public static readonly string SetElementNodeName = CreateSetVariableName(ElementVariableKindName);
        public static readonly string ReleaseElementNodeName = CreateReleaseVariableName(ElementVariableKindName);
        public static readonly string AddressOfElementNodeName = CreateAddressOfVariableName(ElementVariableKindName);

        /// <summary>
        /// Gets the "field" variable kind name.
        /// </summary>
        public const string FieldVariableKindName = "field";

        public static readonly string GetFieldNodeName = CreateGetVariableName(FieldVariableKindName);
        public static readonly string SetFieldNodeName = CreateSetVariableName(FieldVariableKindName);
        public static readonly string ReleaseFieldNodeName = CreateReleaseVariableName(FieldVariableKindName);
        public static readonly string AddressOfFieldNodeName = CreateAddressOfVariableName(FieldVariableKindName);

        #endregion

        #region Special

        /// <summary>
        /// A node name for expressions that refer to a tag.
        /// </summary>
        /// <remarks>
        /// Format:
        /// 
        /// #tag(tag_identifier)
        /// </remarks>
        public const string TagReferenceName = "#tag";

        #endregion

        #endregion

        #region Parser Helpers

        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateParser(Func<ParserState, LNode, IExpression> ParseExpression)
        {
            return new Func<ParserState, LNode, INodeStructure<IExpression>>((state, node) =>
                new LazyNodeStructure<IExpression>(node, () => ParseExpression(state, node)));
        }

        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateLiteralParser(Func<object, IExpression> ParseLiteral)
        {
            return new Func<ParserState, LNode, INodeStructure<IExpression>>((state, node) =>
                new LazyNodeStructure<IExpression>(node, () => ParseLiteral(node.Args[0].Value)));
        }

        #endregion

        #region Expression/Statement Helpers

        /// <summary>
        /// Wraps the given statement in an expression that 
        /// yields a value of type void.
        /// </summary>
        /// <param name="Statement"></param>
        /// <returns></returns>
        public static IExpression ToExpression(IStatement Statement)
        {
            return new InitializedExpression(Statement, VoidExpression.Instance);
        }

        /// <summary>
        /// Converts the given expression to a statement.
        /// Any value yielded by said expression will be discarded.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static IStatement ToStatement(IExpression Expression)
        {
            if (Expression is InitializedExpression)
            {
                var initExpr = (InitializedExpression)Expression;
                return new BlockStatement(new IStatement[] { initExpr.Initialization, ToStatement(initExpr.Value), initExpr.Finalization });
            }
            else if (Expression is SelectExpression)
            {
                var selectExpr = (SelectExpression)Expression;
                return new IfElseStatement(selectExpr.Condition, ToStatement(selectExpr.TrueValue), ToStatement(selectExpr.FalseValue));
            }
            else
            {
                return new ExpressionStatement(Expression);
            }
        }

        /// <summary>
        /// Parses the given node as an expression using the specified state.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseExpression(ParserState State, LNode Node)
        {
            return State.Parser.ExpressionParser.Parse(State, Node).Value;
        }

        /// <summary>
        /// Parses the given sequence of nodes as a sequence of expressions,
        /// using the specified parser state.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<IExpression> ParseExpressions(ParserState State, IEnumerable<LNode> Nodes)
        {
            var parser = State.Parser.ExpressionParser;
            return Nodes.Select(item => parser.Parse(State, item).Value);
        }

        #endregion

        #region Interprocedural control flow

        /// <summary>
        /// Parses a '#return' expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseReturn(ParserState State, LNode Node)
        {
            if (Node.ArgCount == 0)
            {
                return ToExpression(new ReturnStatement());
            }

            var result = ToExpression(new ReturnStatement(ParseExpression(State, Node.Args[0])));
            if (Node.ArgCount > 1)
            {
                return new ErrorExpression(result, new LogEntry(
                    "Invalid '" + ReturnNodeName + "' node",
                    "'" + ReturnNodeName + "' nodes must return zero or one values."));
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Parses a '#throw' expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseThrow(ParserState State, LNode Node)
        {
            if (Node.ArgCount == 0)
            {
                return new ErrorExpression(ToExpression(new AssertStatement(new BooleanExpression(false))), 
                    new LogEntry("Invalid '" + ThrowNodeName + "' node",
                    "'" + ThrowNodeName + "' nodes must throw exactly one value."));
            }

            var result = ToExpression(new ThrowStatement(ParseExpression(State, Node.Args[0])));
            if (Node.ArgCount > 1)
            {
                return new ErrorExpression(result, new LogEntry(
                    "Invalid '" + ThrowNodeName + "' node",
                    "'" + ThrowNodeName + "' nodes must throw exactly one value."));
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Parses an '#assert' expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseAssert(ParserState State, LNode Node)
        {
            if (Node.ArgCount == 0)
            {
                return new ErrorExpression(VoidExpression.Instance,
                    new LogEntry("Invalid '" + AssertNodeName + "' node",
                    "'" + AssertNodeName + "' nodes take exactly one argument."));
            }

            var result = ToExpression(new AssertStatement(ParseExpression(State, Node.Args[0])));
            if (Node.ArgCount > 1)
            {
                return new ErrorExpression(result, new LogEntry(
                    "Invalid '" + AssertNodeName + "' node",
                    "'" + AssertNodeName + "' nodes take exactly one argument."));
            }
            else
            {
                return result;
            }
        }

        #endregion

        #region Intraprocedural control flow

        /// <summary>
        /// Parses a block of sequential flow nodes.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseBlock(ParserState State, LNode Node)
        {
            var exprs = ParseExpressions(State, Node.Args).ToArray();
            var valueExpr = exprs.Select((item, index) => Tuple.Create(item, index))
                                 .LastOrDefault(item => !object.Equals(item.Item1.Type, PrimitiveTypes.Void));
            if (valueExpr == null)
            {
                return ToExpression(new BlockStatement(exprs.Select(ToStatement).ToArray()));
            }
            var initStmts = new BlockStatement(exprs.Take(valueExpr.Item2).Select(ToStatement).ToArray());
            var finalStmts = new BlockStatement(exprs.Skip(valueExpr.Item2 + 1).Select(ToStatement).ToArray());
            return new InitializedExpression(initStmts, valueExpr.Item1, finalStmts);
        }

        /// <summary>
        /// Parses a selection control flow node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseSelect(ParserState State, LNode Node)
        {
            if (Node.ArgCount != 3)
            {
                return new ErrorExpression(VoidExpression.Instance, new LogEntry(
                    "Invalid '" + SelectNodeName + "' node",
                    "'" + SelectNodeName + "' nodes must have exactly three arguments."));
            }

            return new SelectExpression(ParseExpression(State, Node.Args[0]),
                                        ParseExpression(State, Node.Args[1]),
                                        ParseExpression(State, Node.Args[2]));
        }

        /// <summary>
        /// Parses the given tagged statement node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static IExpression ParseTagged(ParserState State, LNode Node, BlockTag Tag)
        {
            // Format:
            //
            // #tagged(tag, body)

            var body = ToStatement(ParseExpression(State, Node.Args[1]));
            return ToExpression(new TaggedStatement(Tag, body));
        }

        /// <summary>
        /// Parses the given while statement node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static IExpression ParseWhile(ParserState State, LNode Node, BlockTag Tag)
        {
            // Format:
            // 
            // #while(tag, condition, body)

            var cond = ParseExpression(State, Node.Args[1]);
            var body = ToStatement(ParseExpression(State, Node.Args[2]));
            return ToExpression(new WhileStatement(Tag, cond, body));
        }

        /// <summary>
        /// Parses the given do-while statement node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public static IExpression ParseDoWhile(ParserState State, LNode Node, BlockTag Tag)
        {
            // Format:
            //
            // #do_while(tag, body, condition)
            
            var body = ToStatement(ParseExpression(State, Node.Args[1]));
            var cond = ParseExpression(State, Node.Args[2]);
            return ToExpression(new DoWhileStatement(Tag, body, cond));
        }

        public static IExpression ParseFor(ParserState State, LNode Node, BlockTag Tag)
        {
            // Format:
            // 
            // #for(tag, initialize, condition, delta, body, finalize)

            var init = ToStatement(ParseExpression(State, Node.Args[1]));
            var cond = ParseExpression(State, Node.Args[2]);
            var delta = ToStatement(ParseExpression(State, Node.Args[3]));
            var body = ToStatement(ParseExpression(State, Node.Args[4]));
            var final = ToStatement(ParseExpression(State, Node.Args[5]));

            return ToExpression(new ForStatement(Tag, init, cond, delta, body, final));
        }

        /// <summary>
        /// Parses the given break node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseBreak(ParserState State, LNode Node)
        {
            return ToExpression(new BreakStatement(ParseBlockTag(State, Node.Args[0])));
        }

        /// <summary>
        /// Parses the given continue node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseContinue(ParserState State, LNode Node)
        {
            return ToExpression(new ContinueStatement(ParseBlockTag(State, Node.Args[0])));
        }

        #region Tagged Node Helpers

        /// <summary>
        /// Gets the tag associated with the given tagged block node.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static string GetTaggedNodeTag(LNode Node)
        {
            // Format:
            //
            // <#tagged_node>(tag, ...)

            return IRParser.GetIdOrString(Node.Args[0]);
        }

        /// <summary>
        /// Creates a parser for tagged nodes.
        /// </summary>
        /// <param name="Parser"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateTaggedNodeParser(Func<LNode, string> GetTag, Func<ParserState, LNode, BlockTag, IExpression> Parse)
        {
            var func = new Func<ParserState, LNode, IExpression>((state, node) =>
            {
                string tagName = GetTag(node);
                var tag = new BlockTag(tagName);
                var oldExprParser = state.Parser.ExpressionParser;
                var exprParser = oldExprParser.WithParser(TagReferenceName, (s, n) =>
                {
                    if (n.ArgCount != 1)
                    {
                        throw new InvalidOperationException("'#tag' blocks must always have exactly one argument.");
                    }

                    if (IRParser.GetIdOrString(n.Args[0]) == tagName)
                    {
                        return new ConstantNodeStructure<IExpression>(n, new TagReferenceExpression(tag));
                    }
                    else
                    {
                        return oldExprParser.Parse(state, n);
                    }
                });
                var newParser = state.Parser.WithExpressionParser(exprParser);
                var newState = state.WithParser(newParser);
                return Parse(newState, node, tag);
            });
            return CreateParser(func);
        }

        /// <summary>
        /// Parses the given block tag node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static BlockTag ParseBlockTag(ParserState State, LNode Node)
        {
            var expr = ParseExpression(State, Node) as TagReferenceExpression;
            if (expr == null)
            {
                throw new InvalidOperationException("Node '" + Node.ToString() + "' was expected to be a valid '#tag' node, but it was not.");
            }

            return expr.Tag;
        }

        #endregion

        #endregion

        #region Ignore operator

        /// <summary>
        /// Parses an 'ignore' node, which computes and then 
        /// discards its body expression.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseIgnore(ParserState State, LNode Node)
        {
            // Format:
            //
            // #ignore(expression)

            var body = ParseExpression(State, Node.Args.Single());

            return ToExpression(ToStatement(body));
        }

        #endregion

        #region Container types

        /// <summary>
        /// Parses the given new-array expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseNewArray(ParserState State, LNode Node)
        {
            var elemTy = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
            var dims = ParseExpressions(State, Node.Args.Slice(1));
            return new NewArrayExpression(elemTy, dims);
        }

        /// <summary>
        /// Parses the given new-vector expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseNewVector(ParserState State, LNode Node)
        {
            var elemTy = State.Parser.TypeReferenceParser.Parse(State, Node.Args[0]).Value;
            var dims = Node.Args.Slice(1).Select(item => Convert.ToInt32(item.Value)).ToArray();
            return new NewVectorExpression(elemTy, dims);
        }

        #endregion

        #region Delegates and calls

        /// <summary>
        /// Parses the given get-delegate node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseGetDelegate(ParserState State, LNode Node)
        {
            // Format:
            //
            // #get_delegate(target, closure)

            var target = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
            var closure = ParseExpression(State, Node.Args[1]);
            return new GetMethodExpression(target, closure, Operator.GetDelegate);
        }

        /// <summary>
        /// Parses the given get-virtual-delegate node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseGetVirtualDelegate(ParserState State, LNode Node)
        {
            // Format:
            //
            // #get_virtual_delegate(target, closure)

            var target = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
            var closure = ParseExpression(State, Node.Args[1]);
            return new GetMethodExpression(target, closure, Operator.GetVirtualDelegate);
        }

        /// <summary>
        /// Parses the given get-extension-delegate node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseGetExtensionDelegate(ParserState State, LNode Node)
        {
            // Format:
            //
            // #get_extension_delegate(target, closure)

            var target = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
            var closure = ParseExpression(State, Node.Args[1]);
            return new GetExtensionMethodExpression(target, closure);
        }

        /// <summary>
        /// Parses the given get-curried-delegate node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseGetCurriedDelegate(ParserState State, LNode Node)
        {
            // Format:
            //
            // #get_curried_delegate(target, closure)

            var target = State.Parser.MethodReferenceParser.Parse(State, Node.Args[0]).Value;
            var closure = ParseExpression(State, Node.Args[1]);
            return new GetMethodExpression(target, closure, Operator.GetCurriedDelegate);
        }

        /// <summary>
        /// Parses the given invocation node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseInvocation(ParserState State, LNode Node)
        {
            // Format:
            //
            // #invoke(target, args...)

            var target = ParseExpression(State, Node.Args[0]);
            var args = ParseExpressions(State, Node.Args.Skip(1));

            return target.CreateDelegateInvocationExpression(args);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Parses the given unary op node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseUnary(ParserState State, LNode Node)
        {
            // Format:
            //
            // #unary("@", operand)

            var op = Operator.Register((string)Node.Args[0].Value);
            var operand = ParseExpression(State, Node.Args[1]);

            return DirectUnaryExpression.CreateUnaryExpression(op, operand);
        }

        /// <summary>
        /// Parses the given binary op node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseBinary(ParserState State, LNode Node)
        {
            // Format:
            //
            // #binary(left, "@", right)
            //
            // where @ is any operator

            var lhs = ParseExpression(State, Node.Args[0]);
            var op = Operator.Register((string)Node.Args[1].Value);
            var rhs = ParseExpression(State, Node.Args[2]);

            return new DirectBinaryExpression(lhs, op, rhs);
        }

        /// <summary>
        /// Creates a parser delegate that parses type binary nodes.
        /// </summary>
        /// <param name="CreateCast"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateTypeBinaryParser(Func<IExpression, IType, IExpression> CreateCastExpression)
        {
            return CreateParser((state, node) =>
            {
                var targetType = state.Parser.TypeReferenceParser.Parse(state, node.Args[0]).Value;
                var castExpr = ParseExpression(state, node.Args[1]);

                return CreateCastExpression(castExpr, targetType);
            });
        }

        #endregion

        #region Constants

        /// <summary>
        /// Defines a parser that parses constant expressions.
        /// The same value is returned, regardless of the node provided.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateConstantParser(IExpression Expression)
        {
            return CreateParser((state, node) => Expression);
        }

        /// <summary>
        /// Parses the given default value constant node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseConstantDefault(ParserState State, LNode Node)
        {
            return new DefaultValueExpression(State.Parser.TypeReferenceParser.Parse(State, Node.Args.Single()).Value);
        }

        #endregion

        #region Variables

        #region Generic

        /// <summary>
        /// Creates a delegate that parses get-variable nodes.
        /// </summary>
        /// <param name="ParseVariable"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateGetVariableParser(Func<ParserState, IEnumerable<LNode>, IVariable> ParseVariable)
        {
            return CreateParser((state, node) => ParseVariable(state, node.Args).CreateGetExpression());
        }

        /// <summary>
        /// Creates a delegate that parses set-variable nodes.
        /// </summary>
        /// <param name="ParseVariable"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateSetVariableParser(Func<ParserState, IEnumerable<LNode>, IVariable> ParseVariable)
        {
            return CreateParser((state, node) =>
            {
                var lhs = ParseVariable(state, node.Args.Slice(0, node.Args.Count - 1));
                var rhs = ParseExpression(state, node.Args.Last);
                return ToExpression(lhs.CreateSetStatement(rhs));
            });
        }

        /// <summary>
        /// Creates a delegate that parses release-variable nodes.
        /// </summary>
        /// <param name="ParseVariable"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateReleaseVariableParser(Func<ParserState, IEnumerable<LNode>, IVariable> ParseVariable)
        {
            return CreateParser((state, node) => ToExpression(ParseVariable(state, node.Args).CreateReleaseStatement()));
        }

        /// <summary>
        /// Creates a delegate that parses address-of-variable nodes.
        /// </summary>
        /// <param name="ParseVariable"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateAddressOfVariableParser(Func<ParserState, IEnumerable<LNode>, IUnmanagedVariable> ParseVariable)
        {
            return CreateParser((state, node) => ParseVariable(state, node.Args).CreateAddressOfExpression());
        }

        #endregion

        #region Fields

        /// <summary>
        /// Parses a field variable node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IUnmanagedVariable ParseFieldVariable(ParserState State, IEnumerable<LNode> Args)
        {
            // Format:
            //
            // #<op>_field(field_reference, target, ...)

            var fieldRef = State.Parser.FieldReferenceParser.Parse(State, Args.First()).Value;
            var target = ParseExpression(State, Args.Skip(1).Single());

            return new FieldVariable(fieldRef, target);
        }

        #endregion

        #region Container elements

        /// <summary>
        /// Parses a container element variable node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IUnmanagedVariable ParseElementVariable(ParserState State, IEnumerable<LNode> Args)
        {
            // Format:
            //
            // #<op>_element(target, index..., ...)

            var target = ParseExpression(State, Args.First());
            var indices = ParseExpressions(State, Args.Skip(1));

            return new ElementVariable(target, indices);
        }

        #endregion

        #region This

        /// <summary>
        /// Creates a parser that parses get-this expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateGetThisParser(IType ThisType)
        {
            return CreateConstantParser(new ThisGetExpression(ThisType));
        }

        /// <summary>
        /// Creates a parser that parses addressof-this expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateAddressOfThisParser(IType ThisType)
        {
            return CreateConstantParser(new ThisAddressOfExpression(ThisType.MakePointerType(PointerKind.ReferencePointer)));
        }

        /// <summary>
        /// Parses the given set-this node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseSetThis(ParserState State, LNode Node)
        {
            var value = ParseExpression(State, Node);

            return ToExpression(new ThisSetStatement(value));
        }

        #endregion

        #region Arguments

        /// <summary>
        /// Creates a parser that parses get-argument expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateGetArgumentParser(IParameter[] Parameters)
        {
            return CreateParser((state, item) => 
            {
                int index = Convert.ToInt32(item.Args[0].Value);
                return new ArgumentVariable(Parameters[index], index).CreateGetExpression();
            });
        }

        /// <summary>
        /// Creates a parser that parses addressof-argument expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateAddressOfArgumentParser(IParameter[] Parameters)
        {
            return CreateParser((state, item) =>
            {
                int index = Convert.ToInt32(item.Args[0].Value);
                return new ArgumentVariable(Parameters[index], index).CreateAddressOfExpression();
            });
        }

        /// <summary>
        /// Creates a parser that parses set-argument statements.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateSetArgumentParser(IParameter[] Parameters)
        {
            return CreateParser((state, item) =>
            {
                int index = Convert.ToInt32(item.Args[0].Value);
                var value = ParseExpression(state, item.Args[1]);
                return ToExpression(new ArgumentVariable(Parameters[index], index).CreateSetStatement(value));
            });
        }

        /// <summary>
        /// Creates a parser that parses release-argument statements.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateReleaseArgumentParser(IParameter[] Parameters)
        {
            return CreateParser((state, item) =>
            {
                int index = Convert.ToInt32(item.Args[0].Value);
                return ToExpression(new ArgumentVariable(Parameters[index], index).CreateReleaseStatement());
            });
        }

        #endregion

        #region Locals

        /// <summary>
        /// Creates a parser that parses get-local expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateGetLocalParser(string Name, LateBoundVariable Variable, ParserState OldState)
        {
            return CreateParser((state, node) =>
            {
                if (node.Args[0].Name.Name == Name)
                {
                    return Variable.CreateGetExpression();
                }
                else
                {
                    return ParseExpression(OldState, node);
                }
            });
        }

        /// <summary>
        /// Creates a parser that parses addressof-argument expressions.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateAddressOfLocalParser(string Name, LateBoundVariable Variable, ParserState OldState)
        {
            return CreateParser((state, node) =>
            {
                if (node.Args[0].Name.Name == Name)
                {
                    return Variable.CreateAddressOfExpression();
                }
                else
                {
                    return ParseExpression(OldState, node);
                }
            });
        }

        /// <summary>
        /// Creates a parser that parses set-argument statements.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateSetLocalParser(string Name, LateBoundVariable Variable, ParserState OldState)
        {
            return CreateParser((state, node) =>
            {
                if (node.Args[0].Name.Name == Name)
                {
                    var value = ParseExpression(state, node.Args[1]);
                    return ToExpression(Variable.CreateSetStatement(value));
                }
                else
                {
                    return ParseExpression(OldState, node);
                }
            });
        }

        /// <summary>
        /// Creates a parser that parses release-argument statements.
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateReleaseLocalParser(string Name, LateBoundVariable Variable, ParserState OldState)
        {
            return CreateParser((state, node) =>
            {
                if (node.Args[0].Name.Name == Name)
                {
                    return ToExpression(Variable.CreateReleaseStatement());
                }
                else
                {
                    return ParseExpression(OldState, node);
                }
            });
        }

        /// <summary>
        /// Parses a single local definition block.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseLocalDefinition(ParserState State, LNode Node)
        {
            // Format:
            // 
            // #def_local(local_name, #member(name, attrs...), type, body)

            string localName = IRParser.GetIdOrString(Node.Args[0]);
            var sig = IRParser.ParseSignature(State, Node.Args[1]);
            var ty = State.Parser.TypeReferenceParser.Parse(State, Node.Args[2]).Value;
            var member = new DescribedVariableMember(sig.Name, ty);
            foreach (var attr in sig.Attributes)
            {
                member.AddAttribute(attr);
            }
            var local = new LateBoundVariable(member);

            var exprParser = State.Parser.ExpressionParser.WithParsers(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IExpression>>>()
            {
                { GetLocalNodeName, CreateGetLocalParser(localName, local, State) },
                { AddressOfLocalNodeName, CreateAddressOfLocalParser(localName, local, State) },
                { SetLocalNodeName, CreateSetLocalParser(localName, local, State) },
                { ReleaseLocalNodeName, CreateReleaseLocalParser(localName, local, State) }
            });

            var newParser = State.Parser.WithExpressionParser(exprParser);
            var newState = State.WithParser(newParser);

            return ParseExpression(newState, Node.Args[3]);
        }

        #endregion

        #endregion

        #region Default Parser

        public static ReferenceParser<IExpression> DefaultExpressionParser
        {
            get
            {
                return new ReferenceParser<IExpression>(new Dictionary<string, Func<ParserState, LNode, INodeStructure<IExpression>>>()
                {
                    // Interprocedural control flow
                    { ReturnNodeName, CreateParser(ParseReturn) },
                    { ThrowNodeName, CreateParser(ParseThrow) },
                    { AssertNodeName, CreateParser(ParseAssert) },

                    // Intraprocedural control flow
                    { BlockNodeName, CreateParser(ParseBlock) },
                    { SelectNodeName, CreateParser(ParseSelect) },
                    { TaggedNodeName, CreateTaggedNodeParser(GetTaggedNodeTag, ParseTagged) },
                    { WhileNodeName, CreateTaggedNodeParser(GetTaggedNodeTag, ParseWhile) },
                    { DoWhileNodeName, CreateTaggedNodeParser(GetTaggedNodeTag, ParseDoWhile) },
                    { ForNodeName, CreateTaggedNodeParser(GetTaggedNodeTag, ParseFor) },
                    { BreakNodeName, CreateParser(ParseBreak) },
                    { ContinueNodeName, CreateParser(ParseContinue) },
                    { TagReferenceName, (state, node) => { throw new InvalidOperationException("Undefined block tag '" + node.Args[0].Name.Name + "'."); }  },
                    
                    // Locals
                    { DefineLocalNodeName, CreateParser(ParseLocalDefinition) },
                    { GetLocalNodeName, (state, node) => { throw new InvalidOperationException("Undefined local '" + IRParser.ParseSignatureName(node.Args[0]) + "'."); }  },
                    { AddressOfLocalNodeName, (state, node) => { throw new InvalidOperationException("Undefined local '" + IRParser.ParseSignatureName(node.Args[0]) + "'."); }  },
                    { SetLocalNodeName, (state, node) => { throw new InvalidOperationException("Undefined local '" + IRParser.ParseSignatureName(node.Args[0]) + "'."); }  },
                    { ReleaseLocalNodeName, (state, node) => { throw new InvalidOperationException("Undefined local '" + IRParser.ParseSignatureName(node.Args[0]) + "'."); }  },

                    // Null expressions
                    { IRParser.NullNodeName, CreateParser((state, node) => null) },

                    // Delegates and invocations
                    { GetDelegateNodeName, CreateParser(ParseGetDelegate) },
                    { GetVirtualDelegateNodeName, CreateParser(ParseGetVirtualDelegate) },
                    { GetExtensionDelegateNodeName, CreateParser(ParseGetExtensionDelegate) },
                    { GetCurriedDelegateNodeName, CreateParser(ParseGetCurriedDelegate) },
                    { InvocationNodeName, CreateParser(ParseInvocation) },

                    // Operators
                    { BinaryNode, CreateParser(ParseBinary) },
                    { UnaryNode, CreateParser(ParseUnary) },
                    { IgnoreNodeName, CreateParser(ParseIgnore) },

                    // Casts
                    { StaticCastNode, CreateTypeBinaryParser((expr, type) => new StaticCastExpression(expr, type)) },
                    { ReinterpretCastNode, CreateTypeBinaryParser((expr, type) => new ReinterpretCastExpression(expr, type)) },
                    { DynamicCastNode, CreateTypeBinaryParser((expr, type) => new DynamicCastExpression(expr, type)) },
                    { AsInstanceNode, CreateTypeBinaryParser((expr, type) => new AsInstanceExpression(expr, type)) },
                    { IsInstanceNode, CreateTypeBinaryParser((expr, type) => new IsExpression(expr, type)) },

                    // Container types
                    { NewArrayName, CreateParser(ParseNewArray) },
                    { NewVectorName, CreateParser(ParseNewVector) },

                    { GetElementNodeName, CreateGetVariableParser(ParseElementVariable) },
                    { SetElementNodeName, CreateSetVariableParser(ParseElementVariable) },
                    { ReleaseElementNodeName, CreateReleaseVariableParser(ParseElementVariable) },
                    { AddressOfElementNodeName, CreateAddressOfVariableParser(ParseElementVariable) },

                    // Fields
                    { GetFieldNodeName, CreateGetVariableParser(ParseFieldVariable) },
                    { SetFieldNodeName, CreateSetVariableParser(ParseFieldVariable) },
                    { ReleaseFieldNodeName, CreateReleaseVariableParser(ParseFieldVariable) },
                    { AddressOfFieldNodeName, CreateAddressOfVariableParser(ParseFieldVariable) },

                    // Constants
                    //  - Bit<n>
                    { ConstantBit8Name, CreateLiteralParser(item => new Bit8Expression(Convert.ToByte(item))) },
                    { ConstantBit16Name, CreateLiteralParser(item => new Bit16Expression(Convert.ToUInt16(item))) },
                    { ConstantBit32Name, CreateLiteralParser(item => new Bit32Expression(Convert.ToUInt32(item))) },
                    { ConstantBit64Name, CreateLiteralParser(item => new Bit64Expression(Convert.ToUInt64(item))) },

                    //  - UInt<n>
                    { ConstantUInt8Name, CreateLiteralParser(item => new UInt8Expression(Convert.ToByte(item))) },
                    { ConstantUInt16Name, CreateLiteralParser(item => new UInt16Expression(Convert.ToUInt16(item))) },
                    { ConstantUInt32Name, CreateLiteralParser(item => new UInt32Expression(Convert.ToUInt32(item))) },
                    { ConstantUInt64Name, CreateLiteralParser(item => new UInt64Expression(Convert.ToUInt64(item))) },

                    //  - Int<n>
                    { ConstantInt8Name, CreateLiteralParser(item => new Int8Expression(Convert.ToSByte(item))) },
                    { ConstantInt16Name, CreateLiteralParser(item => new Int16Expression(Convert.ToInt16(item))) },
                    { ConstantInt32Name, CreateLiteralParser(item => new Int32Expression(Convert.ToInt32(item))) },
                    { ConstantInt64Name, CreateLiteralParser(item => new Int64Expression(Convert.ToInt64(item))) },

                    //  - Float<n>
                    { ConstantFloat32Name, CreateLiteralParser(item => new Float32Expression(Convert.ToSingle(item))) },
                    { ConstantFloat64Name, CreateLiteralParser(item => new Float64Expression(Convert.ToDouble(item))) },

                    //  - Miscellaneous
                    { ConstantBooleanName, CreateLiteralParser(item => new BooleanExpression(Convert.ToBoolean(item))) },
                    { ConstantCharName, CreateLiteralParser(item => new CharExpression(Convert.ToChar(item))) },
                    { ConstantStringName, CreateLiteralParser(item => new StringExpression(Convert.ToString(item))) },
                    { ConstantVoidName, CreateConstantParser(VoidExpression.Instance) },
                    { ConstantNullName, CreateConstantParser(NullExpression.Instance) },

                    //  - Default value
                    { ConstantDefaultName, CreateParser(ParseConstantDefault) }
                });
            }
        }

        #endregion
    }
}
