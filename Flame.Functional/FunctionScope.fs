namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions
open Flame.Compiler.Variables
open Flame.Compiler.Statements
open System.Linq
open System.Collections.Generic

open LazyHelpers

/// Defines a scope as declared by a function.
type FunctionScope private(globalScope : GlobalScope, func : IMethod option) =

    let parameters = lazy (globalScope.GetParameters func)

    new(globalScope) =
        new FunctionScope(globalScope, None)
    new(globalScope, func : IMethod) =
        new FunctionScope(globalScope, Some func)

    /// Gets this local scope's associated global scope.
    member this.Global = 
        globalScope

    /// Gets the function associated with this function scope.
    member this.Function = 
        func

    /// Gets this function scope's parameters.
    member this.Parameters = evalLazy parameters

    /// Finds out if this function scope declares a
    /// parameter with the given name directly.
    member this.DeclaresDirectly name = 
        this.Parameters.ContainsKey name

    /// Gets the parameter with the given name, if any.
    member this.GetVariable name =
        let thisParams = this.Parameters
        if thisParams.ContainsKey(name) then
            Some (thisParams.Item name :> IVariable)
        else
            None