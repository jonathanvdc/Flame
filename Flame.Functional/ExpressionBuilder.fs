namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions
open Flame.Compiler.Statements
open Flame.Compiler.Variables
open Pixie
open System
open System.Collections.Generic
open System.Linq

module ExpressionBuilder =

    /// Conservatively tries to determine whether the given
    /// expression may change the local or global state.
    let ChangesState (value : IExpression) =
        if value.IsConstant then
            false
        else
            match value with
            | :? IVariableNode as varNode ->
                varNode.Action <> VariableNodeAction.Get && varNode.Action <> VariableNodeAction.AddressOf
            | _ ->
                true

    /// Converts the given expression to a statement.
    let rec ToStatement (expression : IExpression) : IStatement =
        if not(ChangesState expression) then // Remove expressions that don't modify any state.
            EmptyStatement.Instance :> IStatement
        else 
            match expression with
            | :? InitializedExpression as initExpr -> 
                new BlockStatement([| 
                                      initExpr.Initialization; 
                                      ToStatement initExpr.Value; 
                                      initExpr.Finalization 
                                    |]) 
                                  :> IStatement

            | :? SelectExpression as selectExpr    -> 
                new IfElseStatement(selectExpr.Condition, 
                                    ToStatement selectExpr.TrueValue, 
                                    ToStatement selectExpr.FalseValue) 
                                   :> IStatement

            | :? VariableExpression as valExpr     ->
                ToStatement valExpr.Expression

            | :? ResultExpression as resultExpr    ->
                ToStatement resultExpr.Expression

            | _                                    ->
                new ExpressionStatement(expression) :> IStatement

    /// Converts the given statement to an expression of type void.
    let ToExpression statement : IExpression =
        new InitializedExpression(statement, VoidExpression.Instance) :> IExpression

    /// Creates a return expression that returns the given value.
    /// Return expressions are of type void.
    let Return value =
        ToExpression(new ReturnStatement(value))

    /// Creates a return expression that returns void.
    /// The return expression is of type void, as well.
    let ReturnVoid =
        ToExpression(new ReturnStatement())

    /// Creates an expression that throws the given value.
    let Throw value =
        ToExpression(new ThrowStatement(value))

    /// Creates an expression that selects one of two expressions
    /// to evaluate based on the given condition.
    let Select (scope : LocalScope) condition trueValue falseValue =
        new SelectExpression(scope.Global.ConversionRules.ConvertImplicit condition PrimitiveTypes.Boolean, trueValue, falseValue) :> IExpression

    /// Creates an if-then expression,
    /// which has type void.
    let If (scope : LocalScope) condition body =
        ToExpression(new IfElseStatement(scope.Global.ConversionRules.ConvertImplicit condition PrimitiveTypes.Boolean, ToStatement body))

    /// Creates a while loop expression,
    /// which has type void.
    let While tag condition body =
        ToExpression(new WhileStatement(tag, condition, ToStatement body))

    /// Creates a do-while loop expression,
    /// which has type void.
    let DoWhile tag body condition =
        ToExpression(new DoWhileStatement(tag, ToStatement body, condition))

    /// Creates a new tagged block expression.
    let Tagged tag body =
        ToExpression(new TaggedStatement(tag, ToStatement body))

    /// Creates a for loop expression,
    /// which has type void.
    let For tag initialization condition delta body =
        ToExpression(new ForStatement(tag, 
                                      ToStatement initialization, 
                                      condition, 
                                      ToStatement delta, 
                                      ToStatement body,
                                      EmptyStatement.Instance))

    /// Creates a break expression.
    let Break (scope : LocalScope) =
        ToExpression(new BreakStatement(scope.ControlTag))

    /// Creates a continue expression.
    let Continue (scope : LocalScope) =
        ToExpression(new ContinueStatement(scope.ControlTag))

    /// Discards this expression's value.
    /// The resulting expression is of type void.
    let Pop value =
        value |> ToStatement
              |> ToExpression

    /// Partitions the given sequence into two new sequences, the first of which containing
    /// all elements in the original sequence that precede the last element that matches the predicate.
    /// The second sequence represents all elements in the original sequence starting at the last element
    /// that matches the predicate.
    let rec private partitionAtLast<'a> (predicate : 'a -> bool) (values : seq<'a>) =
        let head = Seq.takeWhile (predicate >> not) values
        let tail = Seq.skip (Seq.length head) values
        if Seq.isEmpty tail then
            head, tail
        else if Seq.isEmpty head then
            let tailHead = Seq.head tail
            let tailTail = Seq.skip 1 tail
            let nextHead, nextTail = partitionAtLast predicate tailTail
            if Seq.isEmpty nextTail then
                head, tail
            else 
                Seq.append (Seq.append head (Seq.singleton tailHead)) nextHead, nextTail
        else
            let nextHead, nextTail = partitionAtLast predicate tail
            Seq.append head nextHead, nextTail

    /// Creates a block expression whose result is dependent
    /// on the given predicate: the value of the last
    /// child expression that satisfies it will be returned by the block.
    /// If no child expression satisfies the predicate, void is returned.
    let Block (elements : seq<IExpression>) (resultPredicate : IExpression -> bool) : IExpression =
        let firstElems, remainingElems = partitionAtLast resultPredicate elements
        let firstElems = firstElems |> Seq.map ToStatement 
                                    |> Array.ofSeq

        if not(remainingElems.Any()) then
            new BlockStatement(firstElems) |> ToExpression
        else
            let result = remainingElems.First()
            let finalization = remainingElems.Skip(1).Select(ToStatement).ToArray()

            new InitializedExpression(
                new BlockStatement(firstElems),
                result,
                new BlockStatement(finalization)) :> IExpression

    /// Creates a functional block expression whose result is the 
    /// result of the first non-void child in the block, if any.
    /// Otherwise void.
    let ExpressionBlock elements =
        Block elements (fun item -> not(item.Type.Equals(PrimitiveTypes.Void)))

    /// Creates an imperative block expression: said expression will
    /// always return void, unless it contains a `ResultExpression`.
    let VoidBlock elements =
        Block elements (fun item -> item :? ResultExpression)

    /// Creates a comma expression.
    let Comma (elements : seq<IExpression>) =
        new InitializedExpression(new BlockStatement(elements.Take(elements.Count() - 1)
                                                             .Select(ToStatement)
                                                             .ToArray()), 
                                  elements.Last()) :> IExpression

    /// Creates a result expression, which can be used
    /// in imperative-style void blocks.
    let Result expression =
        new ResultExpression(expression) :> IExpression

    /// Creates an expression that first performs an initialization,
    /// then creates its return value, and subsequently performs
    /// a finalization step.
    let Initialize initialization value =
        new InitializedExpression(ToStatement initialization, value) :> IExpression

    /// Creates an expression that first creates its return value,
    /// and then performs a finalization action.
    let Finalize value finalization =
        new InitializedExpression(EmptyStatement.Instance, value, ToStatement finalization) :> IExpression

    /// Creates an expression that first performs an initialization,
    /// then creates its return value, and subsequently performs
    /// a finalization step.
    /// This can be used to create RAII-style blocks.
    let Use initialization value finalization =
        new InitializedExpression(ToStatement initialization, value, ToStatement finalization) :> IExpression

    /// Creates an expression that assigns the given local scope to the given
    /// inner expression.
    let Scope value (scope : LocalScope) =
        new InitializedExpression(EmptyStatement.Instance, value, scope.ReleaseStatement) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantInt8 value =
        new Int8Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantUInt8 value =
        new UInt8Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantInt16 value =
        new Int16Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantUInt16 value =
        new UInt16Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantInt32 value =
        new Int32Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantUInt32 value =
        new UInt32Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantInt64 value =
        new Int64Expression(value) :> IExpression

    /// Creates an expression that captures the given constant integer.
    let ConstantUInt64 value =
        new UInt64Expression(value) :> IExpression

    /// Creates an expression that captures the given constant single-precision float.
    let ConstantFloat32 value =
        new Float32Expression(value) :> IExpression

    /// Creates an expression that captures the given constant double-precision float.
    let ConstantFloat64 value =
        new Float64Expression(value) :> IExpression

    /// Creates an expression that captures the given string constant.
    let ConstantString value =
        new StringExpression(value) :> IExpression

    /// Creates an expression that captures the given character constant.
    let ConstantChar value =
        new CharExpression(value) :> IExpression

    /// Creates an expression that captures the given boolean constant.
    let ConstantBoolean value =
        new BooleanExpression(value) :> IExpression

    /// Creates a null expression.
    let Null = 
        new NullExpression() :> IExpression

    /// Creates a void expression.
    let Void =
        VoidExpression.Instance :> IExpression

    /// Creates an expression that represents an erroneous node, 
    /// and contains the given error message.
    let Error entry value = 
        new ErrorExpression(value, entry) :> IExpression

    /// Creates an expression that represents an erroneous node, 
    /// and contains the given error message.
    /// The resulting expression's type is void.
    let VoidError entry =
        Error entry Void

    /// Creates an expression thaet represents a technically correct node, 
    /// that probably doesn't do what it was intended to do.
    let Warning entry value =
        new WarningExpression(value, entry) :> IExpression

    /// Declares a local variable of the given type, name and body.
    let DeclareLocal (context : LocalScope) (varType : IType) (name : string) =
        if context.DeclaresDirectly name then
            let message = new LogEntry("Variable redefinition", "'" + name + "' is defined more than once.")
            let local = (context.GetVariable name).Value
            local.CreateGetExpression() |> Error message, context
        else
            let scope, declVar = context.DeclareVariable varType name
            declVar.CreateGetExpression(), scope

    /// Tries to get the variable captured by the given expression.
    /// This function will reach across metadata node boundaries. 
    let rec GetVariable (expression : IExpression) =
        match expression with
        | :? IVariableNode              as varExpr -> Some(varExpr.GetVariable())
        | :? IMetadataNode<IExpression> as logNode -> GetVariable(logNode.Value)
        | _                                        -> None

    /// Gets the variable captured by the given expression.
    /// If this cannot be done, an expression variable
    /// is created that wraps the given expression. 
    let GetVariableOrExpressionVariable (expression : IExpression) =
        let result = GetVariable expression
        match result with
        | Some x -> x
        | None   -> new ExpressionVariable(expression) :> IVariable

    /// Assigns the given right hand side to the left hand side.
    let rec Assign (context : LocalScope) (left : IExpression) (right : IExpression) : IExpression =
        match GetVariable left with
        | Some variable ->
            let value    = context.Global.ConversionRules.ConvertImplicit right variable.Type 
            new VariableExpression(new InitializedExpression(variable.CreateSetStatement(value), left),
                                   variable, VariableNodeAction.Set) :> IExpression
        | None          ->
            let message = new LogEntry("Expression assignment", "Could not assign an expression to a non-variable expression.")
            VoidError message

    /// Casts an expression to a type, based on the conversion rules given by the local scope.
    let Cast (context : LocalScope) (left : IExpression) (right : IType) : IExpression =
        if right.Equals(PrimitiveTypes.Void) then
            Void
        else
            context.Global.ConversionRules.ConvertExplicit left right

    /// Declares and binds an expression to a variable.
    let Quickbind (context : LocalScope) (value : IExpression) (name : string) =
        let local, scope = DeclareLocal context value.Type name
        Assign scope local value, scope

    let private GetBinaryExpressionType (rules : IConversionRules) left right =
        if rules.HasImplicitConversion left right then
            right
        else
            left

    let private IsPrimitiveOperation (left : IType) (right : IType) (op : Operator) =
        (left.get_IsEnum() || left.get_IsPrimitive()) && (right.get_IsEnum() || right.get_IsPrimitive())

    let private IsReferenceComparison (left : IType) (right : IType) (op : Operator) =
        (op.Equals(Operator.CheckEquality) || op.Equals(Operator.CheckInequality))
            && (left.Equals(PrimitiveTypes.Null) || left.get_IsPointer() || left.get_IsReferenceType())
            && (right.Equals(PrimitiveTypes.Null) || right.get_IsPointer() || right.get_IsReferenceType())
            && (left.Is(right) || right.Is(left))

    /// Creates a binary expression.
    let Binary (op : Operator) (scope : LocalScope) (left : IExpression) (right : IExpression) : IExpression =
        let lType = left.Type
        let rType = right.Type
        let rules = scope.Global.ConversionRules

        let overload = if IsPrimitiveOperation lType rType op || IsReferenceComparison lType rType op then
                           null
                       else
                           op.GetOperatorOverload([| lType; rType |])

        if overload = null then
            let tType = GetBinaryExpressionType rules lType rType

            let lExpr = rules.ConvertImplicit left tType
            let rExpr = rules.ConvertImplicit right tType
            new DirectBinaryExpression(lExpr, op, rExpr) :> IExpression

        else
            let overloadParams = overload.GetParameters()

            let lExpr, rExpr = if overloadParams.Length = 2 then
                                   rules.ConvertImplicit left overloadParams.[0].ParameterType,
                                   rules.ConvertImplicit right overloadParams.[1].ParameterType
                               else
                                   rules.ConvertImplicit left overload.DeclaringType,
                                   rules.ConvertImplicit right overloadParams.[0].ParameterType

            new DirectBinaryExpression(lExpr, op, rExpr) :> IExpression

    let CoalesceNull (lhs : IExpression) (rhs : IExpression) =
        // Convert `<lhs> ?? <rhs>` to `{ var local = <lhs>; local == default(decltype(local)) ? rhs : lhs }` 

        let local = new LateBoundVariable(lhs.Type)
        new InitializedExpression(
            local.CreateSetStatement(lhs),
            new SelectExpression(new EqualityExpression(local.CreateGetExpression(), new DefaultValueExpression(local.Type)), rhs, local.CreateGetExpression()),
            local.CreateReleaseStatement()) :> IExpression

    let Not expression =
        new NotExpression(expression) :> IExpression

    let Negate expression =
        new NegateExpression(expression) :> IExpression

    let PrefixDelta (scope : LocalScope) (expression : IExpression) (op : Operator) : IExpression =
        let convRules = scope.Global.ConversionRules
        let one       = convRules.ConvertExplicit (ConstantInt8(1y)) expression.Type
        Assign scope expression (Binary op scope expression one)

    let PrefixIncrement scope expression =
        PrefixDelta scope expression Operator.Add

    let PostfixIncrement scope expression =
        let inc = PrefixIncrement scope expression
        Finalize expression inc

    let PrefixDecrement scope expression =
        PrefixDelta scope expression Operator.Subtract

    let PostfixDecrement scope expression =
        let dec = PrefixDecrement scope expression
        Finalize expression dec

    let This (scope : LocalScope) =
        let funcScope = scope.Function
        match funcScope.Function with
        | None ->
            let message = new LogEntry("Bad 'this' access",
                                       "The 'this' parameter can only be accessed from within the scope of a non-static method, constructor or accessor.")
            VoidError message
        | Some func ->
            if func.IsStatic then
                let message = new LogEntry("Bad 'this' access",
                                           "The 'this' parameter cannot be accessed from within a static method, constructor or accessor.")
                VoidError message
            else
                let variable = ThisReferenceVariable.Instance.Create func.DeclaringType
                variable.CreateGetExpression()

    let private createExpectedSignatureDescription (namer : IType -> string) (retType : IType) (argTypes : IType seq) = 
        let descMethod = new Flame.Build.DescribedMethod("", null, retType, true)
        argTypes |> Seq.iteri (fun i x -> descMethod.AddParameter(new Flame.Build.DescribedParameter("param" + string i, x)))
        let deleg      = MethodType.Create descMethod
        namer deleg

    let private createSignatureDiff (namer : IType -> string) (argTypes : IType[]) (target : IMethod) =
        let nodes = if target.IsStatic then [TypeDiffComparer.ToTextNode("static ")] else []
        let nodes = if target.IsConstructor then
                        TypeDiffComparer.ToTextNode("new ") :: TypeDiffComparer.ToTextNode(namer target.DeclaringType) :: nodes
                    else
                        let retType = target.ReturnType
                        TypeDiffComparer.ToTextNode(namer target.ReturnType) 
                        :: TypeDiffComparer.ToTextNode(" ") 
                        :: TypeDiffComparer.ToTextNode(target.FullName)
                        :: nodes
        let methodDiffBuilder = new MethodDiffComparer(new FunctionConverter<IType, string>(namer));
        let nodes = methodDiffBuilder.CompareArguments(argTypes, target) :: nodes
        new MarkupNode("node", nodes |> List.rev |> Seq.ofList) :> IMarkupNode

    /// Creates an expression that represents the invocation of the given function on the
    /// given sequence of arguments. The scope provided is used to apply conversion rules.
    let Invoke (scope : LocalScope) (target : IExpression) (args : IExpression seq) : IExpression =
        let delegates = IntersectionExpression.GetIntersectedExpressions target

        let argTypes  = args |> Seq.map (fun x -> x.Type)
                             |> Array.ofSeq

        match delegates.GetBestDelegate argTypes with
        | null ->
            let matches = target.GetMethodGroup()

            let namer = scope.Global.TypeNamer
            let retType = if Seq.isEmpty matches then PrimitiveTypes.Void else (Seq.head matches).ReturnType
            let expectedSignature = createExpectedSignatureDescription namer retType argTypes

            if not (Seq.isEmpty matches) then
                let failedMatchesList = Seq.map (createSignatureDiff namer argTypes) matches

                let explanationNode = new MarkupNode(NodeConstants.TextNodeType,
                                                     "Method call could not be resolved. " +
                                                     "Expected signature compatible with " + expectedSignature.ToString() +
                                                     ". Incompatible or ambiguous matches:") :> IMarkupNode
                let failedMatchesNode = new MarkupNode("list", failedMatchesList) :> IMarkupNode
                let messageNode = new MarkupNode("entry", Seq.ofArray [| explanationNode; failedMatchesNode |])
                VoidError (new LogEntry("Method resolution error", messageNode))
            else
                VoidError (new LogEntry("Method resolution error", 
                                        "Method call could not be resolved because the invocation's target was not recognized as a function. " +
                                        "Expected signature compatible with " + expectedSignature.ToString() + "."))
        | resolvedDelegate ->
            let delegateParams = resolvedDelegate.GetDelegateParameterTypes()

            let convMapping args = args ||> scope.Global.ConversionRules.ConvertImplicit

            let callArgs = Seq.zip args delegateParams |> Seq.map convMapping

            resolvedDelegate.CreateDelegateInvocationExpression(callArgs)

    /// Analyzes the given expression as the target of a member access operation.
    let GetAccessedExpression (target : IExpression) : AccessedExpression =
        if target = null then
            Global
        else
            let targetType = target.Type
            if targetType.get_IsValueType() then
                Value target
            else if targetType.get_IsReferenceType() || targetType.get_IsPointer() then
                Reference target
            else
                Generic target

    /// Analyzes the given expression as the type member in a member access operation.
    let GetAccessedMember<'a when 'a :> ITypeMember> (targetMember : 'a) (targetType : IType option) : AccessedMember<'a> =
        if targetMember.get_IsExtension() && targetType.IsSome && not(targetMember.DeclaringType.Equals(targetType.Value)) then
            Extension targetMember
        else if targetMember.IsStatic then
            Static targetMember
        else
            Instance targetMember

    /// Accesses a field on a target expression, within the local scope.
    let AccessField (scope : LocalScope) (targetField : IField) (accessedExpr : AccessedExpression) : IExpression =
        let accessedField = GetAccessedMember targetField accessedExpr.Type
        match (accessedField, accessedExpr) with
        | (Static field, _) | (Extension field, _) -> (new FieldVariable(field, null)).CreateGetExpression()
        | (Instance field, Value target)           -> (new ValueTypeFieldVariable(field, GetVariableOrExpressionVariable target)).CreateGetExpression()
        | (Instance field, Reference target)       -> (new FieldVariable(field, target)).CreateGetExpression()
        | (_, _)                                   -> 
            let message = "Could not access " + accessedField.MemberPrefix + " field of " + 
                          (accessedExpr.Describe scope.Global.TypeNamer) + "."
            Error (new LogEntry("Invalid field access", message)) (new UnknownExpression(accessedField.Member.FieldType))