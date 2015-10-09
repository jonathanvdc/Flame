using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
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
        public const string BreakNodeName = "#break";
        public const string ContinueNodeName = "#continue";
        public static readonly string BlockNodeName = CodeSymbols.Braces.Name;

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

        #region Special

        /// <summary>
        /// A node name for expressions that refer to a tag.
        /// </summary>
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

            return Node.Args[0].Name.Name;
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

                    if (n.Args[0].Name.Name == tagName)
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
                    { BreakNodeName, CreateParser(ParseBreak) },
                    { ContinueNodeName, CreateParser(ParseContinue) },
                    { TagReferenceName, (state, node) => { throw new InvalidOperationException("Unknown block tag '" + node.Args[0].Name.Name + "'."); }  },
                    
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
                    { ConstantVoidName, CreateParser((state, node) => VoidExpression.Instance) }
                });
            }
        }

        #endregion
    }
}
