namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions

/// Defines an expression that represents the result of an imperative (void) block.
///
/// This information is discarded on optimization.
type ResultExpression(expression : IExpression) =

    /// Gets this result expression's inner expression.
    member this.Expression =
        expression

    interface INode with
        member this.Emit codeGenerator =
            expression.Emit(codeGenerator);

    interface IExpression with
        member this.Accept visitor =
            let innerExpr = visitor.Visit(expression)

            if innerExpr = expression then
                this :> IExpression
            else
                ResultExpression(innerExpr) :> IExpression

        member this.Evaluate() =
            expression.Evaluate()

        member this.IsConstant =
            expression.IsConstant

        member this.Optimize() =
            expression.Optimize()

        member this.Type =
            expression.Type
