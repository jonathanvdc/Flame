using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
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

            var members = new VList<LNode>();

            try
            {
                // Generate a class for the analysis' results.
                members.Add(
                    F.Call(
                        CodeSymbols.Class,
                        F.Id("Results"),
                        F.List(),
                        F.Braces(
                            F.Call(
                                CodeSymbols.Constructor,
                                F.Missing,
                                F.Id("Results"),
                                F.List(F.Var(ReadOnlyListType(F.Id("Transform")), "applicableRules")),
                                F.Braces(F.Assign(F.Id("ApplicableRules"), F.Id("applicableRules"))))
                            .PlusAttr(F.Id(CodeSymbols.Internal)),
                            F.Var(
                                ReadOnlyListType(F.Id("Transform")),
                                "ApplicableRules")
                            .PlusAttrs(
                                CreateDocComment("<summary>The set of all applicable rules detected by the analysis.</summary>"),
                                F.Id(CodeSymbols.Public),
                                F.Id(CodeSymbols.Readonly))))
                    .PlusAttrs(
                        CreateDocComment("<summary>The results produced by the analysis.</summary>"),
                        F.Id(CodeSymbols.Public),
                        F.Id(CodeSymbols.Sealed)));

                // Generate a method to match instructions to prototype patterns.
                members.Add(
                    SynthesizePrototypePatternMatcher(
                        F.Id(protoMatcherName),
                        prototypePatterns,
                        topLevelNode));

                // Generate a class for each rewrite rule.
                var ruleNames = new Dictionary<RewriteRule, string>();
                var ruleRoots = new Dictionary<RewriteRule, IReadOnlyList<InstructionPattern>>();
                for (int i = 0; i < rules.Count; i++)
                {
                    var name = "Rule" + i.ToString(CultureInfo.InvariantCulture);
                    ruleNames[rules[i]] = name;
                    IReadOnlyList<InstructionPattern> roots;
                    members.Add(
                        SynthesizeRewriteRuleClass(
                            rules[i],
                            name,
                            prototypePatterns,
                            out roots));
                    ruleRoots[rules[i]] = roots;
                }

                // Generate a method that walks the flow graph and figures out
                // which rewrite rules are applicable.
                members.Add(SynthesizeRuleMatcher(ruleNames, prototypePatterns, ruleRoots));

                // Generate a trivial AnalyzeWithUpdates implementation.
                // TODO: do less work here by reusing results from previous analyses.
                members.Add(
                    F.Fn(
                        F.Id("Results"),
                        F.Id("AnalyzeWithUpdates"),
                        F.List(
                            F.Var(F.Id("FlowGraph"), GraphParameterName),
                            F.Var(F.Id("Results"), "previousResults"),
                            F.Var(ReadOnlyListType(F.Id("FlowGraphUpdate")), "updates")),
                        F.Braces(
                            F.Call(
                                CodeSymbols.Return,
                                F.Call("Analyze", F.Id(GraphParameterName)))))
                    .PlusAttrs(
                        CreateDocComment("<inheritdoc/>"),
                        F.Id(CodeSymbols.Public)));

                // Generate the analysis class.
                return F.Call(
                    CodeSymbols.Class,
                    F.Id(analysisName),
                    F.List(F.Of(F.Id("IFlowGraphAnalysis"), F.Dot(analysisName, GSymbol.Get("Results")))),
                    F.Braces(members))
                    .WithAttrs(topLevelNode.Attrs)
                    .PlusAttr(F.Id(CodeSymbols.Sealed));
            }
            catch (MacroApplicationException ex)
            {
                return Reject(sink, ex.At, ex.Message);
            }
        }

        private static LNode CreateDocComment(string comment)
        {
            return F.Call(CodeSymbols.TriviaSLComment, F.Literal("/ " + comment));
        }

        private static LNode SynthesizePrototypePatternMatcher(
            LNode name,
            Dictionary<InstructionPattern, int> patterns,
            LNode topLevelNode)
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
                            boundSymbols);

                        if (check != null)
                        {
                            conditions.Add(check);
                        }
                    }
                }
                else
                {
                    throw new MacroApplicationException(
                        topLevelNode,
                        $"Unsupported prototype kind: '{protoKind}'.");
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

            return F.Fn(
                hashSetType,
                name,
                F.List(F.Var(F.Id("InstructionPrototype"), protoParam)),
                F.Braces(body))
                .WithAttrs(
                    F.Id(CodeSymbols.Private),
                    F.Id(CodeSymbols.Static));
        }

        private static LNode SynthesizeFieldCheck(
            LNode fieldVar,
            LNode prototypeArg,
            Dictionary<Symbol, LNode> boundSymbols)
        {
            if (prototypeArg.IsLiteral)
            {
                return F.Call(CodeSymbols.Eq, fieldVar, prototypeArg);
            }
            else if (prototypeArg.IsId && !prototypeArg.HasSpecialName)
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
                        boundSymbols);

                    if (indexCheck != null)
                    {
                        checks.Add(indexCheck);
                    }
                }
                return checks.Aggregate((x, y) => F.Call(CodeSymbols.And, x, y));
            }
            else if (prototypeArg.IsIdNamed(CodeSymbols.Default))
            {
                return F.Call(CodeSymbols.Eq, fieldVar, F.Dot("DefaultConstant", "Instance"));
            }
            else if (prototypeArg.IsIdNamed("#null"))
            {
                return F.Call(CodeSymbols.Eq, fieldVar, F.Dot("NullConstant", "Instance"));
            }
            else
            {
                throw new MacroApplicationException(
                    prototypeArg,
                    $"Unknown node type: '{(prototypeArg.IsId ? prototypeArg.Name.ToString() : prototypeArg.Target.ToString())}'");
            }
        }

        /// <summary>
        /// Synthesizes a class that represents instances of a rewrite rule.
        /// </summary>
        /// <param name="rule">The rewrite rule to synthesize a class for.</param>
        /// <param name="className">The name of the class to generate.</param>
        /// <param name="patterns">A mapping of prototype patterns to integers.</param>
        /// <param name="ruleRoots">A mapping of rewrite rules to their root instructions.</param>
        /// <returns>A class node.</returns>
        private static LNode SynthesizeRewriteRuleClass(
            RewriteRule rule,
            string className,
            Dictionary<InstructionPattern, int> patterns,
            out IReadOnlyList<InstructionPattern> ruleRoots)
        {
            var members = new VList<LNode>();
            var fieldMapping = new Dictionary<Symbol, LNode>();

            // Generate a 'Matches' predicate method.
            var matchStatements = new List<LNode>();
            var rootInstructions = new List<InstructionPattern>();
            ruleRoots = rootInstructions;
            var matchParams = new List<LNode>();
            var nameToPatternMap = GetNameToPatternMap(rule.Pattern);
            int localCounter = 0;
            for (int i = rule.Pattern.Count - 1; i >= 0; i--)
            {
                var name = rule.Pattern[i].InstructionName;
                if (!fieldMapping.ContainsKey(name))
                {
                    var paramName = GSymbol.Get(name.Name + "Candidate");
                    rootInstructions.Add(rule.Pattern[i]);
                    matchParams.Add(F.Var(F.Id("ValueTag"), paramName));
                    matchStatements.AddRange(
                        CreateRewriteRuleMatcher(
                            name,
                            paramName,
                            nameToPatternMap,
                            patterns,
                            fieldMapping,
                            ref localCounter));
                }
            }
            matchParams.Add(F.Var(F.Id("FlowGraph"), GraphParameterName));
            matchParams.Add(
                F.Var(
                    F.Of(F.Id("Dictionary"), F.Id("ValueTag"), F.Of(F.Id("HashSet"), F.Int32)),
                    PatternMatchesParameterName));

            if (rule.Condition != null && rule.Condition.Calls(CodeSymbols.Braces))
            {
                matchStatements.Add(rule.Condition);
            }
            else
            {
                matchStatements.Add(F.Call(CodeSymbols.Return, rule.Condition ?? F.True));
            }

            foreach (var field in fieldMapping.Values)
            {
                members.Add(field.PlusAttr(F.Id(CodeSymbols.Private)));
            }

            members.Add(
                F.Fn(
                    F.Bool,
                    GSymbol.Get("Matches"),
                    F.List(matchParams),
                    F.Braces(matchStatements))
                .PlusAttr(F.Id(CodeSymbols.Public)));

            // Generate an 'Apply' method.
            members.Add(
                F.Fn(
                    F.Void,
                    GSymbol.Get("Apply"),
                    F.List(F.Var(F.Id("FlowGraphBuilder"), GraphParameterName)),
                    CreateRewriteRuleApplier(rule, fieldMapping))
                .PlusAttrs(
                    F.Id(CodeSymbols.Public),
                    F.Id(CodeSymbols.Override)));

            // Generate the class.
            return F.Call(
                CodeSymbols.Class,
                F.Id(className),
                F.List(F.Id("Transform")),
                F.Braces(members))
                .WithAttrs(F.Id(CodeSymbols.Private));
        }

        private static IReadOnlyDictionary<Symbol, InstructionPattern> GetNameToPatternMap(
            IReadOnlyList<InstructionPattern> patterns)
        {
            var result = new Dictionary<Symbol, InstructionPattern>();
            foreach (var pattern in patterns)
            {
                result[pattern.InstructionName] = pattern;
            }
            return result;
        }

        /// <summary>
        /// Creates a sequence of statements that match on a rewrite
        /// rule. Also matches all reachable rules.
        /// </summary>
        /// <param name="instructionName">
        /// The name of the instruction pattern to match on.
        /// </param>
        /// <param name="parameterName">
        /// The name of the instruction parameter that is the root.
        /// </param>
        /// <param name="nameToPatternMapping">
        /// A mapping of instruction names to instruction patterns.
        /// </param>
        /// <param name="patterns">
        /// A mapping of prototype patterns to integers.
        /// </param>
        /// <param name="fieldMapping">
        /// A mapping of variable names to fields.
        /// </param>
        /// <returns>A sequence of expressions.</returns>
        private static IEnumerable<LNode> CreateRewriteRuleMatcher(
            Symbol instructionName,
            Symbol parameterName,
            IReadOnlyDictionary<Symbol, InstructionPattern> nameToPatternMapping,
            Dictionary<InstructionPattern, int> patterns,
            Dictionary<Symbol, LNode> fieldMapping,
            ref int localCounter)
        {
            // Store the instruction in a field.
            fieldMapping[instructionName] = F.Var(F.Id("ValueTag"), instructionName);
            var results = new List<LNode>();
            results.Add(F.Assign(instructionName, F.Id(parameterName)));

            // Early out if we're dealing with a parameter.
            if (!nameToPatternMapping.ContainsKey(instructionName))
            {
                return results;
            }

            var insnPattern = nameToPatternMapping[instructionName];

            // Check the prototype.
            results.Add(
                ReturnFalseIf(
                    F.Call(
                        CodeSymbols.Not,
                        F.Call(
                            F.Dot(
                                F.Call(CodeSymbols.IndexBracks, F.Id(PatternMatchesParameterName), F.Id(parameterName)),
                                GSymbol.Get("Contains")),
                            F.Literal(patterns[insnPattern])))));

            // Load the actual instruction into a local.
            Symbol insnName;
            results.Add(
                DefineTemporary(
                    ValueToInstruction(GraphParameterName, parameterName),
                    ref localCounter,
                    out insnName));

            // Load the instruction's prototype into a local.
            Symbol prototypeName;
            results.Add(
                DefineTemporary(
                    F.Call(
                        CodeSymbols.Cast,
                        F.Dot(F.Id(insnName), F.Id("Prototype")),
                        F.Id(PrototypeKindToTypeName(insnPattern.PrototypeKind))),
                    ref localCounter,
                    out prototypeName));

            // Analyze the prototype.
            var paramList = fieldNamesAndTypes[insnPattern.PrototypeKind];
            for (int i = 0; i < insnPattern.PrototypeArgs.Count; i++)
            {
                var pattern = insnPattern.PrototypeArgs[i];
                var fieldName = paramList[i].Key;
                var fieldType = paramList[i].Value;
                results.AddRange(
                    CreatePrototypeParameterMatcher(
                        pattern,
                        F.Dot(F.Id(prototypeName), F.Id(fieldName)),
                        fieldType,
                        fieldMapping,
                        ref localCounter));
            }

            // Analyze the instruction arguments.
            for (int i = 0; i < insnPattern.InstructionArgs.Count; i++)
            {
                var arg = insnPattern.InstructionArgs[i];

                if (arg.Kind == InstructionArgumentKind.List)
                {
                    if (i < insnPattern.InstructionArgs.Count - 1)
                    {
                        throw new MacroApplicationException(
                            "The splatting operator (...) may only be used at the end of an instruction's argument list.");
                    }

                    var loadArg = F.Call(
                        F.Dot(insnName, GSymbol.Get("Arguments"), GSymbol.Get("Skip")),
                        F.Literal(i));

                    if (fieldMapping.ContainsKey(arg.Name))
                    {
                        results.Add(
                            ReturnFalseIf(
                                F.Call(
                                    F.Dot(arg.Name, (Symbol)"SequenceEqual"),
                                    loadArg)));
                    }
                    else
                    {
                        fieldMapping[arg.Name] = F.Var(
                            EnumerableType(F.Id("ValueTag")),
                            arg.Name);

                        results.Add(F.Assign(arg.Name, loadArg));
                    }
                }
                else
                {
                    var loadArg = F.Call(
                        CodeSymbols.IndexBracks,
                        F.Dot(insnName, GSymbol.Get("Arguments")),
                        F.Literal(i));

                    if (fieldMapping.ContainsKey(arg.Name))
                    {
                        results.Add(
                            ReturnFalseIf(
                                F.Call(
                                    CodeSymbols.Neq,
                                    F.Id(arg.Name),
                                    loadArg)));
                    }
                    else
                    {
                        Symbol argTempName;
                        results.Add(
                            DefineTemporary(
                                loadArg,
                                ref localCounter,
                                out argTempName));

                        if (nameToPatternMapping.ContainsKey(arg.Name))
                        {
                            results.Add(
                                ReturnFalseIf(
                                    F.Call(
                                        CodeSymbols.Not,
                                        ValueIsInstruction(GraphParameterName, argTempName))));
                        }

                        results.AddRange(
                            CreateRewriteRuleMatcher(
                                arg.Name,
                                argTempName,
                                nameToPatternMapping,
                                patterns,
                                fieldMapping,
                                ref localCounter));
                    }
                }
            }
            return results;
        }

        private static IReadOnlyList<LNode> CreatePrototypeParameterMatcher(
            LNode pattern,
            LNode parameter,
            LNode parameterType,
            Dictionary<Symbol, LNode> fieldMapping,
            ref int localCounter)
        {
            if (pattern.IsId && !pattern.HasSpecialName)
            {
                if (fieldMapping.ContainsKey(pattern.Name))
                {
                    return new[]
                    {
                        ReturnFalseIf(F.Call(CodeSymbols.Neq, parameter, pattern))
                    };
                }
                else
                {
                    fieldMapping[pattern.Name] = F.Var(parameterType, pattern.Name);
                    return new[]
                    {
                        F.Assign(pattern, parameter)
                    };
                }
            }
            else if (pattern.IsLiteral || (pattern.IsId && pattern.HasSpecialName))
            {
                // We can safely elide literal checks because they are always
                // handled at the prototype pattern matching level.
                return EmptyArray<LNode>.Value;
            }
            else if (pattern.Calls(CodeSymbols.AltList))
            {
                // We can elide a list length check here because we already know
                // that the actual list's length matches the pattern list's length
                // here: if it didn't, then the instruction's prototype wouldn't
                // have matched the prototype pattern.
                if (!parameterType.Calls(CodeSymbols.Of, 2))
                {
                    throw new MacroApplicationException(
                        pattern,
                        "list pattern is applied to a field of type '" + parameterType +
                        "'; list patterns can only be applied to list fields.");
                }

                var results = new List<LNode>();
                Symbol listTemp;
                results.Add(DefineTemporary(parameter, ref localCounter, out listTemp));
                for (int i = 0; i < pattern.ArgCount; i++)
                {
                    results.AddRange(
                        CreatePrototypeParameterMatcher(
                            pattern[i],
                            F.Call(CodeSymbols.IndexBracks, F.Id(listTemp), F.Literal(i)),
                            parameterType.Args[1],
                            fieldMapping,
                            ref localCounter));
                }
                return results;
            }
            else
            {
                throw new MacroApplicationException(
                    pattern,
                    "unsupported pattern type: call to '" + pattern.Name.Name + "'.");
            }
        }

        /// <summary>
        /// Creates a method body that applies a rewrite rule.
        /// </summary>
        /// <param name="rule">The rewrite rule to apply.</param>
        /// <param name="fieldMapping">
        /// A mapping of captured variables to the fields that capture them.
        /// </param>
        /// <returns>
        /// A method body.
        /// </returns>
        private static LNode CreateRewriteRuleApplier(
            RewriteRule rule,
            IReadOnlyDictionary<Symbol, LNode> fieldMapping)
        {
            var statements = new List<LNode>();

            Symbol insertionPoint = null;
            foreach (var pattern in rule.Replacement.Reverse())
            {
                var createInsn = CreateInstructionFromPattern(pattern);
                if (fieldMapping.ContainsKey(pattern.InstructionName))
                {
                    statements.Add(
                        F.Assign(
                            ValueToInstruction(GraphParameterName, pattern.InstructionName),
                            createInsn));
                }
                else
                {
                    if (insertionPoint == null)
                    {
                        throw new MacroApplicationException(
                            "the last instruction of a rewrite rule must be a " +
                            $"redefinition, not a new definition; '{pattern.InstructionName}' is a new definition.");
                    }

                    statements.Add(
                        F.Var(
                            F.Missing,
                            pattern.InstructionName,
                            F.Call(
                                F.Dot(
                                    ValueToInstructionRef(GraphParameterName, insertionPoint),
                                    GSymbol.Get("InsertBefore")),
                                createInsn,
                                F.Literal(pattern.InstructionName.Name))));
                }
                insertionPoint = pattern.InstructionName;
            }

            statements.Reverse();
            return F.Braces(statements);
        }

        /// <summary>
        /// Takes an instruction pattern and turns it into an expression
        /// that creates such an instruction.
        /// </summary>
        /// <param name="pattern">The pattern to instantiate.</param>
        /// <returns>An expression that crates an instruction.</returns>
        private static LNode CreateInstructionFromPattern(InstructionPattern pattern)
        {
            int protoArgCount = pattern.PrototypeArgs.Count;
            var protoNamesAndTypes = fieldNamesAndTypes[pattern.PrototypeKind];
            var protoArgs = new List<LNode>();
            for (int i = 0; i < protoArgCount; i++)
            {
                protoArgs.Add(
                    PatternToPrototypeArgument(
                        pattern.PrototypeArgs[i],
                        protoNamesAndTypes[i].Value));
            }

            return CreatePrototypeInstantiation(
                F.Dot(
                    F.Call(
                        F.Dot(PrototypeKindToTypeName(pattern.PrototypeKind), "Create"),
                        protoArgs),
                    GSymbol.Get("Instantiate")),
                pattern.InstructionArgs);
        }

        /// <summary>
        /// Takes a list of instruction arguments and converts it to a
        /// call that instantiates an instruction prototype.
        /// </summary>
        /// <param name="prototype">A prototype to instantiate.</param>
        /// <param name="arguments">
        /// A list of values to feed to the prototype as arguments.
        /// </param>
        /// <returns>
        /// An instruction prototype instantiation.
        /// </returns>
        private static LNode CreatePrototypeInstantiation(
            LNode prototype,
            IReadOnlyList<InstructionArgument> arguments)
        {
            if (arguments.All(x => x.Kind == InstructionArgumentKind.Value))
            {
                return F.Call(prototype, arguments.Select(x => F.Id(x.Name)));
            }
            else
            {
                // Construct two lists. The first contains sequences of arguments.
                // These will all be concatenated at run time. The second contains
                // single-value arguments. These are aggregated into arrays of arguments
                // here.
                var argSequences = new List<LNode>();
                var valueArgs = new List<LNode>();
                foreach (var item in arguments)
                {
                    if (item.Kind == InstructionArgumentKind.List)
                    {
                        // Add the argument value list to the list of argument
                        // sequences.
                        if (valueArgs.Count > 0)
                        {
                            argSequences.Add(
                                F.Call(
                                    CodeSymbols.New,
                                    new[] { F.Call(CodeSymbols.Array) }.Concat(valueArgs)));
                            valueArgs.Clear();
                        }

                        // Add the list to the list of argument sequences.
                        argSequences.Add(F.Id(item.Name));
                    }
                    else
                    {
                        // Add single values to the argument value list.
                        valueArgs.Add(F.Id(item.Name));
                    }
                }

                // Add the argument value list to the list of argument
                // sequences.
                if (valueArgs.Count > 0)
                {
                    argSequences.Add(
                        F.Call(
                            CodeSymbols.New,
                            new[] { F.Call(CodeSymbols.Array) }.Concat(valueArgs)));
                }

                // Now concatenate all the argument sequences.
                return F.Call(
                    prototype,
                    F.Call(
                        F.Dot(
                            argSequences.Aggregate((left, right) => F.Call(F.Dot(left, (Symbol)"Concat"), right)),
                            (Symbol)"ToArray")));
            }
        }

        /// <summary>
        /// Takes a prototype argument pattern and turns it into a
        /// prototype argument expression.
        /// </summary>
        /// <param name="pattern">The pattern to turn into an expression.</param>
        /// <param name="type">The type of the expression to generate.</param>
        /// <returns>A prototype argument expression.</returns>
        private static LNode PatternToPrototypeArgument(
            LNode pattern,
            LNode type)
        {
            if (pattern.Calls(CodeSymbols.AltList))
            {
                var elementType = type.Args[1];
                if (pattern.ArgCount == 0)
                {
                    return F.Dot(F.Of(F.Id("EmptyArray"), elementType), F.Id("Empty"));
                }
                else
                {
                    return F.Call(
                        CodeSymbols.New,
                        new[] { F.Call(F.Of(CodeSymbols.Array, elementType)) }
                        .Concat(
                            pattern.Args.Select(
                                x => PatternToPrototypeArgument(x, elementType))));
                }
            }
            else if (pattern.IsIdNamed(CodeSymbols.Default))
            {
                return F.Dot("DefaultConstant", "Instance");
            }
            else if (pattern.IsIdNamed("#null"))
            {
                return F.Dot("NullConstant", "Instance");
            }
            else
            {
                return pattern;
            }
        }

        /// <summary>
        /// Synthesizes a method that walks the flow graph and figures out
        /// which rewrite rules are applicable.
        /// </summary>
        /// <param name="ruleNames">
        /// A dictionary that maps the set of rules to recognize to their names.
        /// </param>
        /// <param name="prototypePatterns">
        /// A mapping of prototype patterns to their indices.
        /// </param>
        /// <returns>
        /// A method that walks the flow graph and figures out
        /// which rewrite rules are applicable.
        /// </returns>
        private static LNode SynthesizeRuleMatcher(
            IReadOnlyDictionary<RewriteRule, string> ruleNames,
            IReadOnlyDictionary<InstructionPattern, int> prototypePatterns,
            IReadOnlyDictionary<RewriteRule, IReadOnlyList<InstructionPattern>> patternRoots)
        {
            var body = new VList<LNode>();

            // Create the following code snippet:
            //
            //     var prototypeMatches = new Dictionary<ValueTag, HashSet<int>>();
            //     foreach (var instruction in graph.Instructions)
            //     {
            //         prototypeMatches[instruction] = GetPrototypePatternMatches(
            //             instruction.Prototype);
            //     }
            body.Add(
                F.Var(
                    F.Missing,
                    "prototypeMatches",
                    F.Call(
                        CodeSymbols.New,
                        F.Call(
                            F.Of(
                                F.Id("Dictionary"),
                                F.Id("ValueTag"),
                                F.Of(F.Id("HashSet"), F.Int32))))));

            body.Add(
                F.Call(
                    CodeSymbols.ForEach,
                    F.Var(F.Missing, "instruction"),
                    F.Dot(F.Id(GraphParameterName), F.Id("NamedInstructions")),
                    F.Braces(
                        F.Assign(
                            F.Call(CodeSymbols.IndexBracks, F.Id("prototypeMatches"), F.Id("instruction")),
                            F.Call(
                                F.Id("GetPrototypePatternMatches"),
                                F.Dot("instruction", "Prototype"))))));

            // Create an instance of each rewrite rule.
            var ruleInstances = new Dictionary<RewriteRule, string>();
            foreach (var rule in ruleNames)
            {
                var name = "instanceOf" + rule.Value;
                ruleInstances[rule.Key] = name;
                body.Add(F.Var(F.Missing, name, F.Call(CodeSymbols.New, F.Call(F.Id(rule.Value)))));
            }

            // Create a list to put the results in.
            body.Add(
                F.Var(
                    F.Missing,
                    "transforms",
                    F.Call(
                        CodeSymbols.New,
                        F.Call(F.Of(F.Id("List"), F.Id("Transform"))))));

            // We now iterate through all instructions. For each instruction,
            // we figure out which patterns are applicable, if any.
            int localCounter = 0;
            var patternRootTrie = new TrieNode<int, List<RewriteRule>>();
            foreach (var pair in patternRoots)
            {
                var key = pair.Value.Select(x => prototypePatterns[x]).ToArray();
                List<RewriteRule> ruleList;
                if (patternRootTrie.TryGetValue(key, out ruleList))
                {
                    ruleList.Add(pair.Key);
                }
                else
                {
                    patternRootTrie.Set(
                        key,
                        new List<RewriteRule>() { pair.Key });
                }
            }
            body.AddRange(
                SynthesizeRuleMatcherBody(
                    ruleNames,
                    patternRootTrie,
                    ruleInstances,
                    ImmutableList.Create<LNode>(),
                    ref localCounter));

            body.Add(F.Call(CodeSymbols.Return, F.Call(CodeSymbols.New, F.Call(F.Id("Results"), F.Id("transforms")))));

            return F.Fn(
                F.Id("Results"),
                GSymbol.Get("Analyze"),
                F.List(F.Var(F.Id("FlowGraph"), GraphParameterName)),
                F.Braces(body))
                .PlusAttrs(
                    CreateDocComment("<inheritdoc/>"),
                    F.Id(CodeSymbols.Public));
        }

        private static IEnumerable<LNode> SynthesizeRuleMatcherBody(
            IReadOnlyDictionary<RewriteRule, string> ruleNames,
            TrieNode<int, List<RewriteRule>> patternRoots,
            IReadOnlyDictionary<RewriteRule, string> ruleInstances,
            ImmutableList<LNode> accumulatedRoots,
            ref int localCounter)
        {
            var body = new VList<LNode>();
            if (patternRoots.Value != null)
            {
                // Bottom-most matching logic.
                foreach (var rule in patternRoots.Value)
                {
                    body.Add(
                        F.Call(
                            CodeSymbols.If,
                            F.Call(
                                F.Dot(ruleInstances[rule], "Matches"),
                                accumulatedRoots.Concat(
                                    new[]
                                    {
                                        F.Id(GraphParameterName),
                                        F.Id("prototypeMatches")
                                    })),
                            F.Braces(
                                F.Call(F.Dot("transforms", "Add"), F.Id(ruleInstances[rule])),
                                F.Assign(
                                    F.Id(ruleInstances[rule]),
                                    F.Call(CodeSymbols.New, F.Call(ruleNames[rule]))))));
                }
            }

            if (patternRoots.Children.Count > 0)
            {
                Symbol insnVarName;
                var insnVarDef = DefineTemporary(ref localCounter, out insnVarName);

                var loopBody = new VList<LNode>();

                Symbol insnProtoMatchesVarName;
                loopBody.Add(
                    DefineTemporary(
                        F.Call(CodeSymbols.IndexBracks, F.Id("prototypeMatches"), F.Id(insnVarName)),
                        ref localCounter,
                        out insnProtoMatchesVarName));

                foreach (var keyAndNode in patternRoots.Children)
                {
                    loopBody.Add(
                        F.Call(
                            CodeSymbols.If,
                            F.Call(
                                F.Dot(insnProtoMatchesVarName, GSymbol.Get("Contains")),
                                F.Literal(keyAndNode.Key)),
                            F.Braces(
                                SynthesizeRuleMatcherBody(
                                    ruleNames,
                                    keyAndNode.Value,
                                    ruleInstances,
                                    accumulatedRoots.Add(F.Id(insnVarName)),
                                    ref localCounter))));
                }

                body.Add(
                    F.Call(
                        CodeSymbols.ForEach,
                        insnVarDef,
                        F.Dot(GraphParameterName, GSymbol.Get("NamedInstructions")),
                        F.Braces(loopBody)));
            }

            return body;
        }

        private static LNode DefineTemporary(
            ref int localCounter,
            out Symbol name)
        {
            name = GSymbol.Get(
                "temp" + localCounter.ToString(CultureInfo.InvariantCulture));
            localCounter++;
            return F.Var(F.Missing, name);
        }

        private static LNode DefineTemporary(
            LNode value,
            ref int localCounter,
            out Symbol name)
        {
            name = GSymbol.Get(
                "temp" + localCounter.ToString(CultureInfo.InvariantCulture));
            localCounter++;
            return F.Var(F.Missing, name, value);
        }

        private static LNode ValueToInstructionRef(LNode graph, LNode value)
        {
            return F.Call(F.Dot(graph, GSymbol.Get("GetInstruction")), value);
        }

        private static LNode ValueToInstructionRef(Symbol graph, Symbol value)
        {
            return ValueToInstructionRef(F.Id(graph), F.Id(value));
        }

        private static LNode ValueToInstruction(LNode graph, LNode value)
        {
            return F.Dot(ValueToInstructionRef(graph, value), F.Id("Instruction"));
        }

        private static LNode ValueToInstruction(Symbol graph, Symbol value)
        {
            return ValueToInstruction(F.Id(graph), F.Id(value));
        }

        private static LNode ValueIsInstruction(LNode graph, LNode value)
        {
            return F.Call(F.Dot(graph, GSymbol.Get("ContainsInstruction")), value);
        }

        private static LNode ValueIsInstruction(Symbol graph, Symbol value)
        {
            return ValueIsInstruction(F.Id(graph), F.Id(value));
        }

        private static LNode ReturnFalseIf(LNode condition)
        {
            return F.Call(CodeSymbols.If, condition, F.Call(CodeSymbols.Return, F.False));
        }

        private static LNode ReadOnlyListType(LNode elementType)
        {
            return F.Of(F.Id("IReadOnlyList"), elementType);
        }

        private static LNode EnumerableType(LNode elementType)
        {
            return F.Of(F.Id("IEnumerable"), elementType);
        }

        private static readonly Symbol GraphParameterName = GSymbol.Get("graph");

        private static readonly Symbol PatternMatchesParameterName = GSymbol.Get("patternMatches");

        private static readonly LNode ITypeNode = F.Id("IType");
        private static readonly LNode AlignmentNode = F.Id("Alignment");
        private static readonly LNode IMethodNode = F.Id("IMethod");
        private static readonly LNode IFieldNode = F.Id("IField");
        private static readonly LNode MethodLookupNode = F.Id("MethodLookup");
        private static readonly LNode PointerTypeNode = F.Id("PointerType");

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, LNode>>> fieldNamesAndTypes =
            new Dictionary<string, IReadOnlyList<KeyValuePair<string, LNode>>>()
        {
            { "copy", new[] { new KeyValuePair<string, LNode>("ResultType", ITypeNode) } },
            { "box", new[] { new KeyValuePair<string, LNode>("ElementType", ITypeNode) } },
            { "unbox", new[] { new KeyValuePair<string, LNode>("ElementType", ITypeNode) } },
            { "dynamic_cast", new[] { new KeyValuePair<string, LNode>("TargetType", PointerTypeNode) } },
            { "reinterpret_cast", new[] { new KeyValuePair<string, LNode>("TargetType", PointerTypeNode) } },
            { "get_field_pointer", new[] { new KeyValuePair<string, LNode>("Field", IFieldNode) } },
            { "load_field", new[] { new KeyValuePair<string, LNode>("Field", IFieldNode) } },
            { "store_field", new[] { new KeyValuePair<string, LNode>("Field", IFieldNode) } },
            {
                "load",
                new[]
                {
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode),
                    new KeyValuePair<string, LNode>("IsVolatile", F.Bool),
                    new KeyValuePair<string, LNode>("Alignment", AlignmentNode)
                }
            },
            {
                "store",
                new[]
                {
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode),
                    new KeyValuePair<string, LNode>("IsVolatile", F.Bool),
                    new KeyValuePair<string, LNode>("Alignment", AlignmentNode)
                }
            },
            {
                "constant",
                new[]
                {
                    new KeyValuePair<string, LNode>("Value", F.Id("Constant")),
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode)
                }
            },
            {
                "intrinsic",
                new[]
                {
                    new KeyValuePair<string, LNode>("Name", F.String),
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode),
                    new KeyValuePair<string, LNode>("ParameterTypes", ReadOnlyListType(ITypeNode))
                }
            },
            {
                "indirect_call",
                new[]
                {
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode),
                    new KeyValuePair<string, LNode>("ParameterTypes", ReadOnlyListType(ITypeNode))
                }
            },
            {
                "new_delegate",
                new[]
                {
                    new KeyValuePair<string, LNode>("ResultType", ITypeNode),
                    new KeyValuePair<string, LNode>("Callee", IMethodNode),
                    new KeyValuePair<string, LNode>("HasThisArgument", F.Bool),
                    new KeyValuePair<string, LNode>("Lookup", MethodLookupNode)
                }
            },
            {
                "call",
                new[]
                {
                    new KeyValuePair<string, LNode>("Callee", IMethodNode),
                    new KeyValuePair<string, LNode>("Lookup", MethodLookupNode)
                }
            }
        };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> fieldNames =
            fieldNamesAndTypes.ToDictionary<
                KeyValuePair<string, IReadOnlyList<KeyValuePair<string, LNode>>>,
                string,
                IReadOnlyList<string>>(
                    pair => pair.Key,
                    pair => pair.Value.Select(p => p.Key).ToArray());

        private static string PrototypeKindToTypeName(string prototypeKind)
        {
            bool toUpper = true;
            var name = new StringBuilder();
            foreach (var c in prototypeKind)
            {
                if (c == '_')
                {
                    toUpper = true;
                }
                else if (toUpper)
                {
                    name.Append(char.ToUpperInvariant(c));
                    toUpper = false;
                }
                else
                {
                    name.Append(char.ToLowerInvariant(c));
                }
            }
            name.Append("Prototype");
            return name.ToString();
        }

        private static LNode Reject(IMessageSink sink, LNode at, string msg)
        {
            sink.Write(Severity.Error, at, msg);
            return null;
        }
    }
}
