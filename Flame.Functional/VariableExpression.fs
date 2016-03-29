namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions

/// Defines an expression that implements IVariableNode by
/// explicitly containing a variable and variable action.
type VariableExpression(expression : IExpression, variable : IVariable, action : VariableNodeAction) =

    member this.Expression =
        expression

    interface INode with
        member this.Emit codeGenerator =
            expression.Emit(codeGenerator)

        member this.IsConstantNode =
            expression.IsConstantNode

    interface IExpression with
        member this.Accept visitor =
            let innerExpr = visitor.Visit(expression)

            if innerExpr = expression then
                this :> IExpression
            else
                VariableExpression(innerExpr, variable, action) :> IExpression

        member this.Evaluate() =
            expression.Evaluate()

        member this.Optimize() =
            expression.Optimize()

        member this.Type =
            expression.Type

    interface IVariableNode with

        member this.Action =
            action

        member this.GetVariable() =
            variable