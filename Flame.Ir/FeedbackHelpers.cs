using System;
using System.Collections.Generic;
using Loyc.Syntax;
using Pixie;
using Pixie.Code;
using Pixie.Markup;

namespace Flame.Ir
{
    /// <summary>
    /// A collection of functions that help produce wonderful diagnostics,
    /// geared particularly toward LNodes.
    /// </summary>
    public static class FeedbackHelpers
    {
        /// <summary>
        /// Gets a human-readable string that identifies a node
        /// kind.
        /// </summary>
        /// <param name="kind">
        /// A node kind to spell.
        /// </param>
        /// <returns>
        /// A string that identifies the node kind.
        /// </returns>
        public static string SpellNodeKind(LNodeKind node)
        {
            return node.ToString().ToLower();
        }

        /// <summary>
        /// Gets a human-readable string that identifies a node's
        /// kind.
        /// </summary>
        /// <param name="node">
        /// A node whose kind will be spelled out.
        /// </param>
        /// <returns>
        /// A string that identifies the node's kind.
        /// </returns>
        public static string SpellNodeKind(LNode node)
        {
            return SpellNodeKind(node.Kind);
        }

        /// <summary>
        /// Reports a syntax error.
        /// </summary>
        /// <param name="log">
        /// The log to which the error is sent.
        /// </param>
        /// <param name="node">
        /// The offending node.
        /// </param>
        /// <param name="message">
        /// The error message that specifies what went wrong.
        /// </param>
        public static void LogSyntaxError(this ILog log, LNode node, MarkupNode message)
        {
            var entryMessage = node.Range.Length == 0
                ? new MarkupNode[]
                {
                    message
                }
                : new MarkupNode[]
                {
                    message,
                    new HighlightedSource(ToSourceRegion(node.Range))
                };

            log.Log(
                new LogEntry(
                    Severity.Error,
                    "syntax error",
                    entryMessage));
        }

        /// <summary>
        /// Quotes even (second, fourth, sixth, ...) markup elements in bold
        /// and wraps the result in a sequence node.
        /// </summary>
        /// <param name="nodes">The nodes to process.</param>
        /// <returns>A sequence container node.</returns>
        public static Sequence QuoteEven(params MarkupNode[] nodes)
        {
            var results = new MarkupNode[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i % 2 == 1)
                {
                    results[i] = Quotation.CreateBoldQuotation(nodes[i]);
                }
                else
                {
                    results[i] = nodes[i];
                }
            }
            return new Sequence(results);
        }

        /// <summary>
        /// Quotes even (second, fourth, sixth, ...) strings in bold
        /// and wraps the result in a sequence node.
        /// </summary>
        /// <param name="strings">The strings to process.</param>
        /// <returns>A sequence container node.</returns>
        public static Sequence QuoteEven(params string[] strings)
        {
            var results = new MarkupNode[strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                if (i % 2 == 1)
                {
                    results[i] = Quotation.CreateBoldQuotation(strings[i]);
                }
                else
                {
                    results[i] = new Text(strings[i]);
                }
            }
            return new Sequence(results);
        }

        /// <summary>
        /// Asserts that a node has a particular kind. If it does
        /// not, then a message is sent to a log.
        /// </summary>
        /// <param name="node">
        /// A node to inspect.
        /// </param>
        /// <param name="kind">
        /// The expected node kind for <paramref name="node"/>.
        /// </param>
        /// <param name="log">
        /// A log to send error and warning messages to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node is of the specified kind; otherwise, <c>false</c>.
        /// </returns>
        public static bool AssertOfKind(LNode node, LNodeKind kind, ILog log)
        {
            if (node.Kind != kind)
            {
                log.LogSyntaxError(
                    node,
                    QuoteEven(
                        "expected ",
                        SpellNodeKind(kind),
                        " node, but got ",
                        SpellNodeKind(node),
                        " node instead."));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Asserts that a node is a call. If it is
        /// not, then a message is sent to a log.
        /// </summary>
        /// <param name="node">
        /// A node to inspect.
        /// </param>
        /// <param name="log">
        /// A log to send error and warning messages to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node is a call; otherwise, <c>false</c>.
        /// </returns>
        public static bool AssertIsCall(LNode node, ILog log)
        {
            return AssertOfKind(node, LNodeKind.Call, log);
        }

        /// <summary>
        /// Asserts that a node is an id. If it is
        /// not, then a message is sent to a log.
        /// </summary>
        /// <param name="node">
        /// A node to inspect.
        /// </param>
        /// <param name="log">
        /// A log to send error and warning messages to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node is an id; otherwise, <c>false</c>.
        /// </returns>
        public static bool AssertIsId(LNode node, ILog log)
        {
            return AssertOfKind(node, LNodeKind.Id, log);
        }

        /// <summary>
        /// Asserts that a node is a literal. If it is
        /// not, then a message is sent to a log.
        /// </summary>
        /// <param name="node">
        /// A node to inspect.
        /// </param>
        /// <param name="log">
        /// A log to send error and warning messages to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node is a literal; otherwise, <c>false</c>.
        /// </returns>
        public static bool AssertIsLiteral(LNode node, ILog log)
        {
            return AssertOfKind(node, LNodeKind.Literal, log);
        }

        /// <summary>
        /// Asserts that a node has a particular number of arguments.
        /// </summary>
        /// <param name="node">A node to inspect.</param>
        /// <param name="argCount">The number of arguments to expect.</param>
        /// <param name="log">
        /// A log to send error and warning messages to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the node has the desired number of arguments; otherwise, <c>false</c>.
        /// </returns>
        public static bool AssertArgCount(LNode node, int argCount, ILog log)
        {
            if (node.ArgCount != argCount)
            {
                log.LogSyntaxError(
                    node,
                    QuoteEven(
                        "expected a node with ",
                        argCount.ToString(),
                        " arguments, but got one with ",
                        node.ArgCount.ToString(),
                        " instead."));
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Creates a Pixie source region that is equivalent to a
        /// Loyc source range.
        /// </summary>
        /// <param name="range">A source range.</param>
        /// <returns>A source region.</returns>
        public static SourceRegion ToSourceRegion(SourceRange range)
        {
            return new SourceRegion(ToSourceSpan(range));
        }

        /// <summary>
        /// Creates a Pixie source span that is equivalent to a
        /// Loyc source range.
        /// </summary>
        /// <param name="range">A source range.</param>
        /// <returns>A source span.</returns>
        public static SourceSpan ToSourceSpan(SourceRange range)
        {
            return new SourceSpan(
                ToSourceDocument(range.Source),
                range.StartIndex,
                range.Length);
        }

        /// <summary>
        /// Wraps a Loyc source file in a Pixie source document.
        /// </summary>
        /// <param name="sourceFile">A Loyc source file to wrap.</param>
        /// <returns>A Pixie source document.</returns>
        public static SourceDocument ToSourceDocument(ISourceFile sourceFile)
        {
            return new LoycSourceDocument(sourceFile);
        }
    }
}
