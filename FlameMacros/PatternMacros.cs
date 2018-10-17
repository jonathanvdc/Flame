using System;
using System.Collections.Generic;
using System.Linq;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;

namespace FlameMacros
{
    [ContainsMacros]
    public static class PatternMacros
    {
        static LNodeFactory F = new LNodeFactory(new EmptySourceFile("PatternMacros.cs"));

        [LexicalMacro(
            "#rewriteRuleAnalysis(name, { patterns... })",
            "an analysis that proposes to rewrite instructions based on patterns",
            "#rewriteRuleAnalysis")]
        public static LNode RewriteRuleAnalysis(LNode node, IMacroContext sink)
        {
            if (node.ArgCount != 2)
                return Reject(sink, node, "'#rewriteRuleAnalysis' must take exactly two arguments.");

            if (!node.Args[0].IsId)
                return Reject(sink, node.Args[0], "The first argument to '#rewriteRuleAnalysis' must be an identifier.");

            var name = node.Args[0].Name;
            var rules = new List<RewriteRule>();

            foreach (var ruleNode in node.Args[1].Args)
            {
                if (ruleNode.ArgCount < 2 || ruleNode.ArgCount > 3)
                {
                    return Reject(sink, ruleNode, "Each rewrite rule takes at least two and at most three arguments.");
                }

                var fromPattern = ParsePattern(ruleNode.Args[0], sink);
                var toPattern = ParsePattern(ruleNode.Args[1], sink);
                var condition = ruleNode.ArgCount == 3 ? ruleNode.Args[2] : F.Literal(true);

                if (fromPattern == null || toPattern == null)
                {
                    return null;
                }

                rules.Add(new RewriteRule(fromPattern, toPattern, condition));
            }

            return SynthesizeAnalysis(name, rules, node, sink);
        }

        private static IReadOnlyList<InstructionPattern> ParsePattern(
            LNode instructions,
            IMessageSink sink)
        {
            if (!instructions.Calls(CodeSymbols.Braces))
            {
                sink.Write(
                    Severity.Error,
                    instructions,
                    "Patterns must be lists of instructions delimited by braces.");
                return null;
            }

            var pattern = new List<InstructionPattern>();
            foreach (var insn in instructions.Args)
            {
                var insnPattern = InstructionPattern.Parse(insn, sink);
                if (insnPattern == null)
                {
                    return null;
                }

                pattern.Add(insnPattern);
            }
            return pattern;
        }

        private static LNode SynthesizeAnalysis(
            Symbol analysisName,
            IReadOnlyList<RewriteRule> rules,
            LNode topLevelNode,
            IMessageSink sink)
        {
            // The naive way to create an analysis that points out all
            // possible transformations is to simply iterate over every
            // instruction and figure out if that instruction is the root
            // of a rewrite rule pattern.
            //
            // That's actually what we'll do, but we can be smart about
            // it to avoid re-analyzing instructions. Specifically, what
            // we'll do is categorize instruction patterns into equivalent
            // patterns wrt prototypes such that given two equivalent patterns
            // either both match a prototype or neither matches a prototype.
            //
            // To differentiate between categories of equivalent patterns,
            // we will assign a unique number ot each pattern.
            //
            // At run time, we will iterate through the set of all instruction
            // prototypes in the graph and construct a mapping of prototypes
            // to the patterns that match those prototypes.
            //
            // Once we have that mapping, we will examine each instruction
            // and figure out which rewrite rules match that instruction.

            // List all unique prototype patterns.
            var prototypePatterns = new Dictionary<InstructionPattern, int>(
                InstructionPatternPrototypeComparer.Instance);
            foreach (var item in rules)
            {
                foreach (var insnPattern in item.Pattern)
                {
                    if (!prototypePatterns.ContainsKey(insnPattern))
                    {
                        prototypePatterns[insnPattern] = prototypePatterns.Count;
                    }
                }
            }

            // Generate a function to determine which prototype patterns
            // match a particular prototype.
            var protoMatcherName = "GetPrototypePatternMatches";
            var protoMatcher = SynthesizePrototypePatternMatcher(
                F.Id(protoMatcherName),
                prototypePatterns,
                topLevelNode,
                sink);

            return F.Call(
                CodeSymbols.Class,
                F.Id(analysisName),
                F.List(),
                F.Braces(
                    protoMatcher));
        }

        private static LNode SynthesizePrototypePatternMatcher(
            LNode name,
            Dictionary<InstructionPattern, int> patterns,
            LNode topLevelNode,
            IMessageSink sink)
        {
            // We'll split on prototype kinds and then just compose a linear
            // sequence of tests.
            var testsByKind = new Dictionary<string, List<LNode>>();
            var protoParam = F.Id("prototype");
            var matchSetVar = F.Id("matchSet");
            foreach (var item in patterns)
            {
                List<LNode> tests;
                string protoKind = item.Key.PrototypeKind;
                var protoVar = F.Id(protoKind + "Proto");
                if (!testsByKind.TryGetValue(protoKind, out tests))
                {
                    tests = new List<LNode>();
                    testsByKind[protoKind] = tests;
                    tests.Add(
                        F.Var(
                            F.Missing,
                            protoVar,
                            F.Call(
                                CodeSymbols.Cast,
                                protoParam,
                                F.Id(PrototypeKindToTypeName(protoKind)))));
                }

                var conditions = new List<LNode>();
                var boundSymbols = new Dictionary<Symbol, LNode>();

                IReadOnlyList<string> fields;
                if (fieldNames.TryGetValue(protoKind, out fields))
                {
                    for (int i = 0; i < item.Key.PrototypeArgs.Count; i++)
                    {
                        var check = SynthesizeFieldCheck(
                            F.Dot(protoVar, F.Id(fields[i])),
                            item.Key.PrototypeArgs[i],
                            boundSymbols,
                            sink);

                        if (check != null)
                        {
                            conditions.Add(check);
                        }
                    }
                }
                else
                {
                    sink.Write(
                        Severity.Error,
                        topLevelNode,
                        $"Unsupported prototype kind: '{protoKind}'.");
                    return null;
                }

                var addMatch = F.Call(F.Dot(matchSetVar, F.Id("Add")), F.Literal(item.Value));
                if (conditions.Count == 0)
                {
                    tests.Add(addMatch);
                }
                else
                {
                    tests.Add(
                        F.Call(
                            CodeSymbols.If,
                            conditions.Aggregate((x, y) => F.Call(CodeSymbols.And, x, y)),
                            addMatch));
                }
            }

            var hashSetType = F.Of(F.Id("HashSet"), F.Id(CodeSymbols.Int32));
            var body = new List<LNode>();
            body.Add(
                F.Var(
                    F.Missing,
                    matchSetVar,
                    F.Call(CodeSymbols.New, F.Call(hashSetType))));

            var testBody = F.Braces();
            foreach (var pair in testsByKind)
            {
                body.Add(
                    F.Call(
                        CodeSymbols.If,
                        F.Call(CodeSymbols.Is, protoParam, F.Id(PrototypeKindToTypeName(pair.Key))),
                        F.Braces(pair.Value),
                        testBody));
            }
            body.Add(F.Call(CodeSymbols.Return, matchSetVar));

            return F.Fn(hashSetType, name, F.List(F.Var(F.Id("InstructionPrototype"), protoParam)), F.Braces(body));
        }

        private static LNode SynthesizeFieldCheck(
            LNode fieldVar,
            LNode prototypeArg,
            Dictionary<Symbol, LNode> boundSymbols,
            IMessageSink sink)
        {
            if (prototypeArg.IsLiteral)
            {
                return F.Call(CodeSymbols.Eq, fieldVar, prototypeArg);
            }
            else if (prototypeArg.IsId)
            {
                LNode firstOccurence;
                if (boundSymbols.TryGetValue(prototypeArg.Name, out firstOccurence))
                {
                    return F.Call(CodeSymbols.Eq, fieldVar, firstOccurence);
                }
                else
                {
                    boundSymbols[prototypeArg.Name] = fieldVar;
                    return null;
                }
            }
            else if (prototypeArg.Calls(CodeSymbols.AltList))
            {
                var checks = new List<LNode>();
                checks.Add(F.Call(CodeSymbols.Eq, F.Dot(fieldVar, F.Id("Count")), F.Literal(prototypeArg.ArgCount)));
                for (int i = 0; i < prototypeArg.ArgCount; i++)
                {
                    var indexCheck = SynthesizeFieldCheck(
                        F.Call(CodeSymbols.IndexBracks, fieldVar, F.Literal(i)),
                        prototypeArg.Args[i],
                        boundSymbols,
                        sink);

                    if (indexCheck != null)
                    {
                        checks.Add(indexCheck);
                    }
                }
                return checks.Aggregate((x, y) => F.Call(CodeSymbols.And, x, y));
            }
            else
            {
                sink.Write(
                    Severity.Error,
                    prototypeArg,
                    $"Unknown call target: '{prototypeArg.Target}'");
                return null;
            }
        }

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> fieldNames =
            new Dictionary<string, IReadOnlyList<string>>()
        {
            { "copy", new[] { "ResultType" } },
            { "intrinsic", new[] { "Name", "ResultType", "ParameterTypes" } }
        };

        private static string PrototypeKindToTypeName(string prototypeKind)
        {
            return char.ToUpperInvariant(prototypeKind[0])
                + prototypeKind.Substring(1).ToLowerInvariant()
                + "Prototype";
        }

        private static LNode Reject(IMessageSink sink, LNode at, string msg)
        {
            sink.Write(Severity.Error, at, msg);
            return null;
        }
    }
}
