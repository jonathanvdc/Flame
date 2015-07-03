namespace Flame.Functional

open Flame
open Flame.Compiler

type IConversionRules = 
    /// Finds out whether a value of the given source type
    /// can be converted implicitly to the given target type.
    abstract member HasImplicitConversion : IType -> IType -> bool

    /// Converts the given expression to the given type implicitly.
    abstract member ConvertImplicit : IExpression -> IType -> IExpression

    /// Converts the given expression to the given type explicitly.
    abstract member ConvertExplicit : IExpression -> IType -> IExpression
