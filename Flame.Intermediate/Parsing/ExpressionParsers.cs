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
        public const string SelectNodeName = "#select";
        public static readonly string BlockNodeName = CodeSymbols.Braces.Name;

        #endregion

        #region Parser Helpers

        public static Func<ParserState, LNode, INodeStructure<IExpression>> CreateParser(Func<ParserState, LNode, IExpression> ParseExpression)
        {
            return new Func<ParserState, LNode, INodeStructure<IExpression>>((state, node) =>
                new LazyNodeStructure<IExpression>(node, () => ParseExpression(state, node)));
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
            else
            {
                var result = ToExpression(new ReturnStatement(ParseExpression(State, Node.Args[0])));
                if (Node.ArgCount > 0)
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
        }

        /// <summary>
        /// Parses a '#throw' expression node.
        /// </summary>
        /// <param name="State"></param>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IExpression ParseThrow(ParserState State, LNode Node)
        {
            var result = ToExpression(new ThrowStatement(ParseExpression(State, Node.Args[0])));
            if (Node.ArgCount > 0)
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
            var exprs = Node.Args.Select(item => ParseExpression(State, item)).ToArray();
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

                    // Intraprocedural control flow
                    { BlockNodeName, CreateParser(ParseBlock) },
                    { SelectNodeName, CreateParser(ParseSelect) }
                });
            }
        }

        #endregion
    }
}
